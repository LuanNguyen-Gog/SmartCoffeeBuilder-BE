using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.Payment;
using SmartCoffeeBuilder.Service.DTOs.Responses.Payment;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class PaymentService : IPaymentService
{
    // payOS giới hạn description tối đa 25 ký tự.
    private const int PayOsDescriptionMaxLength = 25;

    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ICollection<SubscriptionPlanResponse>> GetPlansAsync(AccountRole? targetRole = null)
    {
        var plans = await _unitOfWork.GetRepository<SubscriptionPlan>().GetListAsync(
            predicate: p => p.IsActive && (targetRole == null || p.TargetRole == targetRole),
            orderBy: q => q.OrderBy(p => p.TargetRole).ThenBy(p => p.Price));

        return plans.Select(SubscriptionPlanResponse.From).ToList();
    }

    public async Task<CreatePaymentResponse> CreateSubscriptionPaymentAsync(long accountId, CreateSubscriptionPaymentRequest request)
    {
        var plan = await _unitOfWork.GetRepository<SubscriptionPlan>()
                .SingleOrDefaultAsync(predicate: p => p.Id == request.PlanId && p.IsActive)
            ?? throw new KeyNotFoundException($"Không tìm thấy gói subscription với id {request.PlanId}.");

        var account = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với id {accountId}.");

        if (account.Role != plan.TargetRole)
            throw new InvalidOperationException(
                $"Gói '{plan.Name}' dành cho role '{plan.TargetRole}', tài khoản hiện tại là '{account.Role}'.");

        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);

        // Đã có link thanh toán còn hạn cho đúng gói này → trả lại link cũ thay vì tạo giao dịch mới.
        // Chống trường hợp bấm "Thanh toán" nhiều lần, hoặc bấm cancel rồi bấm thanh toán lại trước
        // khi hết 15 phút — cả hai đều sinh ra nhiều mã/nhiều subscription pending nếu không có bước này.
        var reusable = await FindReusablePendingTransactionAsync(
            accountId, PaymentPurpose.subscription, planId: plan.Id, postId: null, expirationSeconds);
        if (reusable != null)
        {
            _logger.LogInformation(
                "Reused pending payOS payment link. Account {AccountId}, plan {PlanId}, orderCode {OrderCode}",
                accountId, plan.Id, reusable.OrderCode);
            return ToPaymentResponse(reusable, expirationSeconds);
        }

        var subscription = new Subscription
        {
            AccountId = accountId,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationInDays),
            Status = SubscriptionStatus.pending,
            PaidAmount = 0
        };
        await _unitOfWork.GetRepository<Subscription>().InsertAsync(subscription);
        await _unitOfWork.CommitAsync();

        var payOs = CreatePayOsClient();
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var description = TruncateForPayOs(plan.Name);
        var expiredAt = DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

        var paymentData = new PaymentData(
            orderCode: orderCode,
            amount: (int)plan.Price,
            description: description,
            items: new List<ItemData> { new(plan.Name, 1, (int)plan.Price) },
            returnUrl: GetRequiredSetting("PayOs:ReturnUrl"),
            cancelUrl: GetRequiredSetting("PayOs:CancelUrl"),
            expiredAt: expiredAt);

        CreatePaymentResult link;
        try
        {
            link = await payOs.createPaymentLink(paymentData);
        }
        catch (Exception ex)
        {
            // Không tạo được link → huỷ luôn subscription pending vừa tạo để không rác DB.
            subscription.Status = SubscriptionStatus.cancelled;
            subscription.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Subscription>().Update(subscription);
            await _unitOfWork.CommitAsync();
            _logger.LogError(ex, "payOS createPaymentLink failed for account {AccountId}, plan {PlanId}", accountId, plan.Id);
            throw new InvalidOperationException("Không thể tạo liên kết thanh toán payOS. Vui lòng thử lại sau.");
        }

        var transaction = new PaymentTransaction
        {
            SubscriptionId = subscription.Id,
            AccountId = accountId,
            Purpose = PaymentPurpose.subscription,
            OrderCode = orderCode,
            PaymentLinkId = link.paymentLinkId,
            CheckoutUrl = link.checkoutUrl,
            QrCode = link.qrCode,
            Amount = plan.Price,
            Description = $"Thanh toán gói {plan.Name}",
            Status = PaymentTransactionStatus.pending
        };
        await _unitOfWork.GetRepository<PaymentTransaction>().InsertAsync(transaction);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "Created payOS payment link. Account {AccountId}, plan {PlanId}, orderCode {OrderCode}",
            accountId, plan.Id, orderCode);

        return new CreatePaymentResponse
        {
            Purpose = PaymentPurpose.subscription.ToString(),
            SubscriptionId = subscription.Id,
            OrderCode = orderCode,
            PaymentLinkId = link.paymentLinkId,
            CheckoutUrl = link.checkoutUrl,
            QrCode = link.qrCode,
            Amount = plan.Price,
            ExpiredAt = expiredAt
        };
    }

    public async Task<CreatePaymentResponse> CreatePostBoostPaymentAsync(long accountId, CreatePostBoostRequest request)
    {
        if (request.Days < 1 || request.Days > 90)
            throw new ArgumentException("Số ngày đẩy bài phải từ 1 đến 90.");

        var post = await _unitOfWork.GetRepository<Post>().SingleOrDefaultAsync(
                predicate: p => p.Id == request.PostId,
                include: q => q.Include(p => p.ProjectShopOwner).ThenInclude(pr => pr.Owner))
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {request.PostId}.");

        if (post.ProjectShopOwner.Owner.AccountId != accountId)
            throw new InvalidOperationException("Chỉ chủ quán sở hữu bài đăng mới mua được lượt đẩy bài.");

        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"Bài đăng đang ở trạng thái '{post.Status}', chỉ đẩy được bài đang mở.");

        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);

        // Đã có link thanh toán còn hạn cho đúng bài đăng này → trả lại link cũ, không tạo mã mới.
        var reusable = await FindReusablePendingTransactionAsync(
            accountId, PaymentPurpose.post_boost, planId: null, postId: post.Id, expirationSeconds);
        if (reusable != null)
        {
            _logger.LogInformation(
                "Reused pending payOS post-boost link. Account {AccountId}, post {PostId}, orderCode {OrderCode}",
                accountId, post.Id, reusable.OrderCode);
            return ToPaymentResponse(reusable, expirationSeconds);
        }

        var pricePerDay = _configuration.GetValue("PayOs:PostBoostPricePerDay", 20_000m);
        var amount = pricePerDay * request.Days;

        var payOs = CreatePayOsClient();
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var description = TruncateForPayOs($"Day bai #{post.Id}");
        var expiredAt = DateTimeOffset.UtcNow.AddSeconds(expirationSeconds).ToUnixTimeSeconds();

        var paymentData = new PaymentData(
            orderCode: orderCode,
            amount: (int)amount,
            description: description,
            items: new List<ItemData> { new($"Đẩy bài {request.Days} ngày", 1, (int)amount) },
            returnUrl: GetRequiredSetting("PayOs:ReturnUrl"),
            cancelUrl: GetRequiredSetting("PayOs:CancelUrl"),
            expiredAt: expiredAt);

        CreatePaymentResult link;
        try
        {
            link = await payOs.createPaymentLink(paymentData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "payOS createPaymentLink failed for post boost. Account {AccountId}, post {PostId}", accountId, post.Id);
            throw new InvalidOperationException("Không thể tạo liên kết thanh toán payOS. Vui lòng thử lại sau.");
        }

        var transaction = new PaymentTransaction
        {
            AccountId = accountId,
            Purpose = PaymentPurpose.post_boost,
            PostId = post.Id,
            BoostDays = request.Days,
            OrderCode = orderCode,
            PaymentLinkId = link.paymentLinkId,
            CheckoutUrl = link.checkoutUrl,
            QrCode = link.qrCode,
            Amount = amount,
            Description = $"Đẩy bài đăng #{post.Id} nổi bật {request.Days} ngày",
            Status = PaymentTransactionStatus.pending
        };
        await _unitOfWork.GetRepository<PaymentTransaction>().InsertAsync(transaction);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "Created payOS post-boost link. Account {AccountId}, post {PostId}, days {Days}, orderCode {OrderCode}",
            accountId, post.Id, request.Days, orderCode);

        return new CreatePaymentResponse
        {
            Purpose = PaymentPurpose.post_boost.ToString(),
            PostId = post.Id,
            OrderCode = orderCode,
            PaymentLinkId = link.paymentLinkId,
            CheckoutUrl = link.checkoutUrl,
            QrCode = link.qrCode,
            Amount = amount,
            ExpiredAt = expiredAt
        };
    }

    public async Task<SubscriptionResponse?> GetActiveSubscriptionAsync(long accountId)
    {
        var now = DateTime.UtcNow;
        var subscription = await _unitOfWork.GetRepository<Subscription>().SingleOrDefaultAsync(
            predicate: s => s.AccountId == accountId && s.Status == SubscriptionStatus.active && s.EndDate > now,
            orderBy: q => q.OrderByDescending(s => s.EndDate),
            include: q => q.Include(s => s.Plan));

        return subscription == null ? null : SubscriptionResponse.From(subscription);
    }

    public async Task<ICollection<SubscriptionResponse>> GetSubscriptionHistoryAsync(long accountId)
    {
        var subscriptions = await _unitOfWork.GetRepository<Subscription>().GetListAsync(
            predicate: s => s.AccountId == accountId,
            orderBy: q => q.OrderByDescending(s => s.CreatedAt),
            include: q => q.Include(s => s.Plan));

        return subscriptions.Select(SubscriptionResponse.From).ToList();
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(long accountId, long? orderCode, string? paymentLinkId)
    {
        if (orderCode == null && string.IsNullOrWhiteSpace(paymentLinkId))
            throw new ArgumentException("Cần cung cấp orderCode hoặc paymentLinkId.");

        var transaction = await FindTransactionAsync(orderCode, paymentLinkId)
            ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán.");

        if (transaction.AccountId != accountId)
            throw new UnauthorizedAccessException("Bạn không có quyền xem giao dịch này.");

        return PaymentStatusResponse.From(transaction);
    }

    public async Task<PaymentStatusResponse> CancelPaymentAsync(long accountId, long orderCode)
    {
        var transaction = await FindTransactionAsync(orderCode, null)
            ?? throw new KeyNotFoundException($"Không tìm thấy giao dịch với orderCode {orderCode}.");

        if (transaction.AccountId != accountId)
            throw new UnauthorizedAccessException("Bạn không có quyền huỷ giao dịch này.");

        // Đã ở trạng thái cuối → idempotent, trả nguyên trạng (FE có thể gọi lại nhiều lần).
        if (transaction.Status != PaymentTransactionStatus.pending)
            return PaymentStatusResponse.From(transaction);

        bool claimed;
        await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                claimed = await TryClaimPendingTransactionAsync(transaction.Id, PaymentTransactionStatus.cancelled);
                if (claimed)
                {
                    CancelPendingSubscriptionIfAny(transaction, DateTime.UtcNow);
                    await _unitOfWork.CommitAsync();
                }
                await _unitOfWork.CommitTransactionAsync(dbTransaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(dbTransaction);
                throw;
            }
        }

        if (!claimed)
        {
            // Thua race với webhook (VD payOS vừa báo paid ngay trước đó) — trả trạng thái mới nhất, KHÔNG huỷ.
            var latest = await FindTransactionAsync(orderCode, null) ?? transaction;
            return PaymentStatusResponse.From(latest);
        }

        // Huỷ link phía payOS để không còn thanh toán được nữa. Thiếu bước này thì user bấm "Huỷ"
        // trên FE xong vẫn có thể lỡ quét/trả tiền qua link cũ — payOS trừ tiền thật nhưng webhook
        // sẽ bị bỏ qua vì giao dịch nội bộ đã ở trạng thái cancelled, mất tiền mà không kích hoạt gì.
        try
        {
            var payOs = CreatePayOsClient();
            await payOs.cancelPaymentLink(orderCode, "Người dùng huỷ giao dịch.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "payOS cancelPaymentLink thất bại cho orderCode {OrderCode} (giao dịch vẫn được đánh dấu huỷ nội bộ, cần đối soát thủ công nếu user vẫn trả được tiền).",
                orderCode);
        }

        transaction.Status = PaymentTransactionStatus.cancelled;
        _logger.LogInformation("Payment cancelled. OrderCode {OrderCode}", orderCode);
        return PaymentStatusResponse.From(transaction);
    }

    public async Task<string> HandleWebhookAsync(WebhookType webhook)
    {
        var payOs = CreatePayOsClient();
        WebhookData data;
        try
        {
            data = payOs.verifyPaymentWebhookData(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "payOS webhook signature verification failed.");
            throw new ArgumentException("Webhook không hợp lệ (sai chữ ký).");
        }

        var transaction = await FindTransactionAsync(data.orderCode, data.paymentLinkId);
        if (transaction == null)
        {
            // payOS gửi payload test (orderCode=123) khi đăng ký webhook URL — phải trả 200.
            _logger.LogInformation(
                "payOS webhook verified but no matching transaction. OrderCode {OrderCode}", data.orderCode);
            return "Webhook hợp lệ nhưng không khớp giao dịch nội bộ.";
        }

        var isPaid = webhook.success
            && string.Equals(webhook.code, "00", StringComparison.OrdinalIgnoreCase)
            && string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        // KHÔNG early-return dựa trên transaction.Status đọc được ở trên — giá trị này có thể stale
        // nếu một webhook khác (payOS retry gửi trùng) đang xử lý đồng thời. Quyền xử lý được "chốt"
        // atomic bên trong ProcessPaymentOutcomeAsync (UPDATE ... WHERE status = pending ở tầng DB)
        // để đảm bảo chỉ đúng MỘT lần gọi được phép kích hoạt subscription / cộng ngày boost.
        var message = await ProcessPaymentOutcomeAsync(transaction, isPaid);

        _logger.LogInformation(
            "payOS webhook processed. OrderCode {OrderCode}, isPaid {IsPaid}, message {Message}",
            transaction.OrderCode, isPaid, message);

        return message;
    }

    public async Task ConfirmWebhookAsync(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentException("Webhook URL không được để trống.");

        var payOs = CreatePayOsClient();
        try
        {
            await payOs.confirmWebhook(webhookUrl.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "payOS confirmWebhook failed for {WebhookUrl}", webhookUrl);
            throw new InvalidOperationException("Không thể xác nhận webhook URL với payOS.");
        }
    }

    public async Task ExpireOverdueSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;
        var subscriptionRepo = _unitOfWork.GetRepository<Subscription>();

        // Subscription active đã quá hạn → expired.
        var overdue = await subscriptionRepo.GetListAsync(
            predicate: s => s.Status == SubscriptionStatus.active && s.EndDate <= now);
        foreach (var subscription in overdue)
        {
            subscription.Status = SubscriptionStatus.expired;
            subscription.UpdatedAt = now;
        }
        subscriptionRepo.UpdateRange(overdue);
        if (overdue.Count > 0)
            await _unitOfWork.CommitAsync();

        // Giao dịch pending mà link payOS chắc chắn đã hết hạn (quá hạn 1 giờ) → huỷ kèm subscription pending.
        // Dùng claim atomic thay vì đọc-rồi-ghi để không đụng độ nếu đúng lúc webhook (payOS gửi trễ)
        // hoặc user bấm cancel cũng đang xử lý cùng giao dịch này.
        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);
        var staleBefore = now.AddSeconds(-expirationSeconds).AddHours(-1);
        var staleTransactions = await _unitOfWork.GetRepository<PaymentTransaction>().GetListAsync(
            predicate: t => t.Status == PaymentTransactionStatus.pending && t.CreatedAt <= staleBefore,
            include: q => q.Include(t => t.Subscription));

        var cancelledCount = 0;
        foreach (var transaction in staleTransactions)
        {
            if (!await TryClaimPendingTransactionAsync(transaction.Id, PaymentTransactionStatus.cancelled))
                continue; // đã bị webhook/cancel xử lý ngay trước khi job này chạy tới

            cancelledCount++;
            CancelPendingSubscriptionIfAny(transaction, now);
        }
        if (cancelledCount > 0)
            await _unitOfWork.CommitAsync();

        if (overdue.Count > 0 || cancelledCount > 0)
            _logger.LogInformation(
                "Subscription maintenance: {Expired} expired, {Cancelled} stale pending transactions cancelled.",
                overdue.Count, cancelledCount);
    }

    /// <summary>
    /// Chốt kết quả webhook cho MỘT giao dịch: giành quyền xử lý atomic rồi mới áp side-effect
    /// (kích hoạt subscription / cộng ngày boost, hoặc huỷ subscription pending nếu thất bại).
    /// Bọc trong DB transaction để claim + side-effect + save cùng thành công hoặc cùng rollback.
    /// </summary>
    private async Task<string> ProcessPaymentOutcomeAsync(PaymentTransaction transaction, bool isPaid)
    {
        var newStatus = isPaid ? PaymentTransactionStatus.paid : PaymentTransactionStatus.failed;
        bool claimed;

        await using (var dbTransaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                claimed = await TryClaimPendingTransactionAsync(transaction.Id, newStatus);
                if (claimed)
                {
                    if (isPaid)
                    {
                        if (transaction.Purpose == PaymentPurpose.post_boost)
                            ApplyPostBoostSideEffect(transaction);
                        else
                            await ApplySubscriptionSideEffectAsync(transaction);
                    }
                    else
                    {
                        CancelPendingSubscriptionIfAny(transaction, DateTime.UtcNow);
                    }

                    await _unitOfWork.CommitAsync();
                }

                await _unitOfWork.CommitTransactionAsync(dbTransaction);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(dbTransaction);
                throw;
            }
        }

        if (!claimed)
            return "Giao dịch đã được xử lý trước đó.";

        if (!isPaid)
            return "Đã ghi nhận giao dịch thất bại.";

        return transaction.Purpose == PaymentPurpose.post_boost
            ? "Thanh toán thành công — bài đăng đã được đẩy nổi bật."
            : "Thanh toán thành công — subscription đã được kích hoạt.";
    }

    /// <summary>
    /// Kích hoạt subscription sau khi payOS xác nhận đã thanh toán (transaction đã được claim atomic
    /// trước đó — hàm này CHỈ áp side-effect, không tự set lại Status của transaction).
    /// Nếu account đang có gói active còn hạn thì cộng nối tiếp từ EndDate hiện tại.
    /// </summary>
    private async Task ApplySubscriptionSideEffectAsync(PaymentTransaction transaction)
    {
        var now = DateTime.UtcNow;
        var subscription = transaction.Subscription
            ?? throw new InvalidOperationException(
                $"Giao dịch #{transaction.Id} purpose subscription nhưng không gắn subscription.");

        var currentActiveEnd = await _unitOfWork.GetRepository<Subscription>().SingleOrDefaultAsync(
            selector: s => (DateTime?)s.EndDate,
            predicate: s => s.AccountId == subscription.AccountId
                && s.Id != subscription.Id
                && s.Status == SubscriptionStatus.active
                && s.EndDate > now,
            orderBy: q => q.OrderByDescending(s => s.EndDate));

        var startDate = currentActiveEnd ?? now;
        subscription.Status = SubscriptionStatus.active;
        subscription.StartDate = startDate;
        subscription.EndDate = startDate.AddDays(subscription.Plan.DurationInDays);
        subscription.PaidAmount = transaction.Amount;
        subscription.UpdatedAt = now;
        _unitOfWork.GetRepository<Subscription>().Update(subscription);
    }

    /// <summary>
    /// Chốt boost sau khi payOS xác nhận đã thanh toán (transaction đã được claim atomic trước đó):
    /// cộng dồn số ngày vào BoostedUntil (đang boost dở thì nối tiếp từ hạn hiện tại, hết boost thì
    /// tính từ bây giờ).
    /// </summary>
    private void ApplyPostBoostSideEffect(PaymentTransaction transaction)
    {
        var now = DateTime.UtcNow;
        var post = transaction.Post;
        if (post == null)
        {
            // Bài đăng đã bị xoá trước khi webhook về — vẫn ghi nhận giao dịch paid để đối soát.
            _logger.LogWarning(
                "payOS webhook paid cho post boost nhưng post không còn. Transaction #{Id}", transaction.Id);
            return;
        }

        var baseTime = post.BoostedUntil.HasValue && post.BoostedUntil.Value > now
            ? post.BoostedUntil.Value
            : now;
        post.BoostedUntil = baseTime.AddDays(transaction.BoostDays ?? 0);
        post.UpdatedAt = now;
        _unitOfWork.GetRepository<Post>().Update(post);
    }

    /// <summary>Huỷ subscription pending đi kèm một giao dịch (nếu có) — dùng khi giao dịch bị huỷ/thất bại.</summary>
    private void CancelPendingSubscriptionIfAny(PaymentTransaction transaction, DateTime now)
    {
        if (transaction.Subscription is not { Status: SubscriptionStatus.pending }) return;

        transaction.Subscription.Status = SubscriptionStatus.cancelled;
        transaction.Subscription.UpdatedAt = now;
        _unitOfWork.GetRepository<Subscription>().Update(transaction.Subscription);
    }

    /// <summary>
    /// Chuyển trạng thái giao dịch pending → newStatus bằng một câu UPDATE ... WHERE status = 'pending'
    /// duy nhất — atomic ở tầng DB (Postgres khoá row trong lúc UPDATE), nên khi 2 nguồn xử lý cùng lúc
    /// (payOS gửi webhook trùng do retry, hoặc webhook và user-cancel đụng nhau) chỉ đúng MỘT lệnh gọi
    /// "thắng" (affected == 1); lệnh còn lại thấy status đã đổi nên affected == 0 và không được làm gì
    /// tiếp. Đây là truy vấn có điều kiện mà GenericRepository.Update (attach + set toàn bộ Modified,
    /// không có WHERE) không diễn đạt được, nên dùng thẳng _unitOfWork.Context cho riêng thao tác này.
    /// </summary>
    private async Task<bool> TryClaimPendingTransactionAsync(long transactionId, PaymentTransactionStatus newStatus)
    {
        var affected = await _unitOfWork.Context.Set<PaymentTransaction>()
            .Where(t => t.Id == transactionId && t.Status == PaymentTransactionStatus.pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Status, newStatus)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow));

        return affected == 1;
    }

    /// <summary>
    /// Tìm giao dịch pending còn trong hạn payOS (ExpirationSeconds) của account cho đúng mục tiêu
    /// (planId cho subscription / postId cho post_boost) — dùng để tái sử dụng link cũ thay vì gọi
    /// payOS tạo giao dịch mới mỗi lần user bấm "Thanh toán" (double-click, hoặc bấm cancel rồi bấm
    /// lại trong lúc link cũ vẫn còn hạn).
    /// </summary>
    private Task<PaymentTransaction?> FindReusablePendingTransactionAsync(
        long accountId, PaymentPurpose purpose, long? planId, long? postId, int expirationSeconds)
    {
        var notExpiredAfter = DateTime.UtcNow.AddSeconds(-expirationSeconds);

        return _unitOfWork.GetRepository<PaymentTransaction>().SingleOrDefaultAsync(
            predicate: t => t.AccountId == accountId
                && t.Purpose == purpose
                && t.Status == PaymentTransactionStatus.pending
                && t.CreatedAt > notExpiredAfter
                && (planId == null || (t.Subscription != null && t.Subscription.PlanId == planId))
                && (postId == null || t.PostId == postId),
            orderBy: q => q.OrderByDescending(t => t.CreatedAt));
    }

    private static CreatePaymentResponse ToPaymentResponse(PaymentTransaction t, int expirationSeconds) => new()
    {
        Purpose = t.Purpose.ToString(),
        SubscriptionId = t.SubscriptionId,
        PostId = t.PostId,
        OrderCode = t.OrderCode,
        PaymentLinkId = t.PaymentLinkId,
        CheckoutUrl = t.CheckoutUrl,
        QrCode = t.QrCode,
        Amount = t.Amount,
        // ExpiredAt gốc không được lưu riêng — suy lại từ CreatedAt + ExpirationSeconds, đúng bằng
        // giá trị đã gửi cho payOS lúc tạo link (trừ khi config ExpirationSeconds vừa đổi giữa chừng).
        ExpiredAt = new DateTimeOffset(t.CreatedAt, TimeSpan.Zero).AddSeconds(expirationSeconds).ToUnixTimeSeconds()
    };

    private Task<PaymentTransaction?> FindTransactionAsync(long? orderCode, string? paymentLinkId)
    {
        return _unitOfWork.GetRepository<PaymentTransaction>().SingleOrDefaultAsync(
            predicate: t =>
                (orderCode != null && t.OrderCode == orderCode) ||
                (orderCode == null && t.PaymentLinkId == paymentLinkId),
            include: q => q
                .Include(t => t.Subscription).ThenInclude(s => s!.Plan)
                .Include(t => t.Post));
    }

    private static string TruncateForPayOs(string value) =>
        value.Length > PayOsDescriptionMaxLength ? value[..PayOsDescriptionMaxLength] : value;

    private PayOS CreatePayOsClient() => new(
        GetRequiredSetting("PayOs:ClientId"),
        GetRequiredSetting("PayOs:ApiKey"),
        GetRequiredSetting("PayOs:ChecksumKey"));

    private string GetRequiredSetting(string key) =>
        _configuration[key] ?? throw new InvalidOperationException($"Missing configuration: {key}");
}
