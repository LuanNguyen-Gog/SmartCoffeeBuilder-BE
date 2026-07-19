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
        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);
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

        var pricePerDay = _configuration.GetValue("PayOs:PostBoostPricePerDay", 20_000m);
        var amount = pricePerDay * request.Days;

        var payOs = CreatePayOsClient();
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var description = TruncateForPayOs($"Day bai #{post.Id}");
        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);
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

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(long? orderCode, string? paymentLinkId)
    {
        if (orderCode == null && string.IsNullOrWhiteSpace(paymentLinkId))
            throw new ArgumentException("Cần cung cấp orderCode hoặc paymentLinkId.");

        var transaction = await FindTransactionAsync(orderCode, paymentLinkId)
            ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán.");

        return PaymentStatusResponse.From(transaction);
    }

    public async Task<PaymentStatusResponse> CancelPaymentAsync(long orderCode)
    {
        var transaction = await FindTransactionAsync(orderCode, null)
            ?? throw new KeyNotFoundException($"Không tìm thấy giao dịch với orderCode {orderCode}.");

        // Đã ở trạng thái cuối → trả nguyên trạng (idempotent, FE có thể gọi lại nhiều lần).
        if (transaction.Status != PaymentTransactionStatus.pending)
            return PaymentStatusResponse.From(transaction);

        MarkTransactionFailed(transaction, PaymentTransactionStatus.cancelled);
        await _unitOfWork.CommitAsync();

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

        if (transaction.Status != PaymentTransactionStatus.pending)
            return "Giao dịch đã được xử lý trước đó.";

        var isPaid = webhook.success
            && string.Equals(webhook.code, "00", StringComparison.OrdinalIgnoreCase)
            && string.Equals(data.code, "00", StringComparison.OrdinalIgnoreCase);

        if (isPaid)
        {
            if (transaction.Purpose == PaymentPurpose.post_boost)
            {
                ActivatePostBoost(transaction);
                await _unitOfWork.CommitAsync();
                _logger.LogInformation(
                    "Post {PostId} boosted via payOS webhook. OrderCode {OrderCode}",
                    transaction.PostId, transaction.OrderCode);
                return "Thanh toán thành công — bài đăng đã được đẩy nổi bật.";
            }

            await ActivateSubscriptionAsync(transaction);
            await _unitOfWork.CommitAsync();
            _logger.LogInformation(
                "Subscription {SubscriptionId} activated via payOS webhook. OrderCode {OrderCode}",
                transaction.SubscriptionId, transaction.OrderCode);
            return "Thanh toán thành công — subscription đã được kích hoạt.";
        }

        MarkTransactionFailed(transaction, PaymentTransactionStatus.failed);
        await _unitOfWork.CommitAsync();
        _logger.LogInformation(
            "payOS webhook reported failure. OrderCode {OrderCode}, code {Code}", transaction.OrderCode, data.code);
        return "Đã ghi nhận giao dịch thất bại.";
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
        var transactionRepo = _unitOfWork.GetRepository<PaymentTransaction>();

        // Subscription active đã quá hạn → expired.
        var overdue = await subscriptionRepo.GetListAsync(
            predicate: s => s.Status == SubscriptionStatus.active && s.EndDate <= now);
        foreach (var subscription in overdue)
        {
            subscription.Status = SubscriptionStatus.expired;
            subscription.UpdatedAt = now;
        }
        subscriptionRepo.UpdateRange(overdue);

        // Giao dịch pending mà link payOS chắc chắn đã hết hạn (quá hạn 1 giờ) → huỷ kèm subscription pending.
        var expirationSeconds = _configuration.GetValue("PayOs:ExpirationSeconds", 900);
        var staleBefore = now.AddSeconds(-expirationSeconds).AddHours(-1);
        var staleTransactions = await transactionRepo.GetListAsync(
            predicate: t => t.Status == PaymentTransactionStatus.pending && t.CreatedAt <= staleBefore,
            include: q => q.Include(t => t.Subscription));
        foreach (var transaction in staleTransactions)
            MarkTransactionFailed(transaction, PaymentTransactionStatus.cancelled);

        if (overdue.Count > 0 || staleTransactions.Count > 0)
        {
            await _unitOfWork.CommitAsync();
            _logger.LogInformation(
                "Subscription maintenance: {Expired} expired, {Cancelled} stale pending transactions cancelled.",
                overdue.Count, staleTransactions.Count);
        }
    }

    /// <summary>
    /// Kích hoạt subscription sau khi payOS xác nhận đã thanh toán.
    /// Nếu account đang có gói active còn hạn thì cộng nối tiếp từ EndDate hiện tại.
    /// </summary>
    private async Task ActivateSubscriptionAsync(PaymentTransaction transaction)
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

        transaction.Status = PaymentTransactionStatus.paid;
        transaction.UpdatedAt = now;
        _unitOfWork.GetRepository<PaymentTransaction>().Update(transaction);
    }

    /// <summary>
    /// Chốt boost sau khi payOS xác nhận đã thanh toán: cộng dồn số ngày vào BoostedUntil
    /// (đang boost dở thì nối tiếp từ hạn hiện tại, hết boost thì tính từ bây giờ).
    /// </summary>
    private void ActivatePostBoost(PaymentTransaction transaction)
    {
        var now = DateTime.UtcNow;
        var post = transaction.Post;
        if (post == null)
        {
            // Bài đăng đã bị xoá trước khi webhook về — vẫn ghi nhận giao dịch paid để đối soát.
            _logger.LogWarning(
                "payOS webhook paid cho post boost nhưng post không còn. Transaction #{Id}", transaction.Id);
        }
        else
        {
            var baseTime = post.BoostedUntil.HasValue && post.BoostedUntil.Value > now
                ? post.BoostedUntil.Value
                : now;
            post.BoostedUntil = baseTime.AddDays(transaction.BoostDays ?? 0);
            post.UpdatedAt = now;
            _unitOfWork.GetRepository<Post>().Update(post);
        }

        transaction.Status = PaymentTransactionStatus.paid;
        transaction.UpdatedAt = now;
        _unitOfWork.GetRepository<PaymentTransaction>().Update(transaction);
    }

    /// <summary>Đánh dấu giao dịch cancelled/failed; subscription pending đi kèm (nếu có) cũng bị huỷ.</summary>
    private void MarkTransactionFailed(PaymentTransaction transaction, PaymentTransactionStatus status)
    {
        var now = DateTime.UtcNow;
        transaction.Status = status;
        transaction.UpdatedAt = now;
        _unitOfWork.GetRepository<PaymentTransaction>().Update(transaction);

        if (transaction.Subscription is { Status: SubscriptionStatus.pending })
        {
            transaction.Subscription.Status = SubscriptionStatus.cancelled;
            transaction.Subscription.UpdatedAt = now;
            _unitOfWork.GetRepository<Subscription>().Update(transaction.Subscription);
        }
    }

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
