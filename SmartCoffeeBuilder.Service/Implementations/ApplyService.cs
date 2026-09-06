using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ApplyService : IApplyService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Apply> _repository;
    private readonly INotificationService _notificationService;

    public ApplyService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Apply>();
        _notificationService = notificationService;
    }

    public async Task<PaginationResponse<ApplyResponse>> GetAllAsync(
        Guid accountId,
        int pageNumber = 1, int pageSize = 10,
        Guid? postId = null, Guid? serviceProviderProfileId = null, string? status = null)
    {
        ApplicationStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' is not valid. Allowed: pending, accepted, rejected.");
            st = parsed;
        }

        // Quyền xem đi THẲNG VÀO QUERY, không lọc sau khi lấy về (TotalItems sẽ sai).
        // Proposal + EstimatedDurationDays là nội dung chào thầu: để hở thì provider chỉ cần đổi
        // postId là đọc được bài của đối thủ — đúng thứ C5 đã bịt ở thread comment báo giá.
        var isAdmin = await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(
                a => a.ServiceProviderProfile.DeletedAt == null      // ẩn hồ sơ của provider đã xoá mềm
                     && a.Post.ProjectShopOwner.DeletedAt == null     // và của bài thuộc dự án đã xoá mềm
                     && (isAdmin
                         || a.ServiceProviderProfile.AccountId == accountId
                         || a.Post.ProjectShopOwner.Owner.AccountId == accountId)
                     && (postId == null || a.PostId == postId)
                     && (serviceProviderProfileId == null || a.ServiceProviderProfileId == serviceProviderProfileId)
                     && (st == null || a.Status == st),
                include: BuildApplyInclude())
            .OrderByDescending(a => a.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ApplyResponse>(
            paged.Items.Select(ApplyResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ApplyResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        await EnsurePartyAsync(accountId, id);

        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id
                            && a.ServiceProviderProfile.DeletedAt == null
                            && a.Post.ProjectShopOwner.DeletedAt == null,
            include: BuildApplyInclude())
            ?? throw new KeyNotFoundException($"No application found with id {id}.");

        return ApplyResponse.From(application);
    }

    /// <summary>
    /// Include dùng chung cho hai đường đọc hồ sơ ứng tuyển. Nạp kèm hồ sơ năng lực, lịch sử
    /// engagement + review (để tính điểm theo hạng mục) và các bản báo giá — đây là bộ dữ liệu
    /// owner cần để CHỌN provider, đúng yêu cầu review 3.
    ///
    /// Chấp nhận join rộng vì đây là màn hình cân nhắc, mỗi bài đăng chỉ vài hồ sơ; đổi lại FE
    /// không phải gọi thêm 3 API cho mỗi dòng danh sách.
    /// </summary>
    /// <summary>
    /// Owner chỉ chốt được provider đã ĐI KHẢO SÁT THỰC TẾ — đó là dữ liệu để so giữa nhiều hồ sơ.
    ///
    /// Chặn ở accept chứ không chặn ở lúc nộp hồ sơ: survey neo vào <c>apply_id</c> nên bản ghi
    /// apply phải có trước đã; và khảo sát là buổi đi hiện trường, provider cần ứng tuyển trước
    /// để hẹn được lịch với owner. Accept mới là chỗ luật này có nghĩa.
    ///
    /// Chỉ áp cho bài đăng CÓ pha thiết kế (design / both). Bài construction thuần không có bước
    /// khảo sát — khớp với <c>SurveyService.EnsureCanSurveyEngagementAsync</c>, vốn đã chặn
    /// engagement contract type 'construction' tạo survey.
    ///
    /// Hẹn lịch suông không tính: <c>surveyed_at == null</c> nghĩa là chưa đi, owner chưa có gì để đọc.
    /// </summary>
    private async Task EnsureSurveySubmittedAsync(Guid applyId, ServiceKind serviceKind)
    {
        if (serviceKind == ServiceKind.construction) return;

        var surveys = await _unitOfWork.GetRepository<Survey>()
            .GetListAsync(predicate: s => s.ApplyId == applyId);

        if (surveys.Count == 0)
            throw new InvalidOperationException(
                $"Application #{applyId} has no survey yet — a post with scope '{serviceKind}' " +
                "requires the provider to survey the site before the application can be accepted.");

        if (!surveys.Any(s => s.SurveyedAt != null))
            throw new InvalidOperationException(
                $"Application #{applyId} only has a scheduled survey and no actual visit yet (surveyed_at is still empty) — " +
                "it cannot be accepted.");
    }

    private static Func<IQueryable<Apply>, IIncludableQueryable<Apply, object>> BuildApplyInclude() =>
        q => q.Include(a => a.Post)
              .Include(a => a.Quotations)
              .Include(a => a.Surveys)
              .Include(a => a.ServiceProviderProfile)
                  .ThenInclude(p => p.ProjectWorkings)
                  .ThenInclude(e => e.Reviews)
                  .ThenInclude(r => r.ReviewScores);

    public async Task<ApplyResponse> ApplyAsync(Guid accountId, CreateApplyRequest request)
    {
        var post = await _unitOfWork.GetRepository<Post>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.PostId)
            ?? throw new KeyNotFoundException($"No post found with id {request.PostId}.");

        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"The post is in status '{post.Status}' and is not accepting applications.");

        if (post.SubmissionDeadline.HasValue && post.SubmissionDeadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("The application deadline for this post has passed.");

        // Hồ sơ provider lấy theo account đang đăng nhập — không nhận id từ client.
        var provider = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .SingleOrDefaultAsync(predicate: s => s.AccountId == accountId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException("The signed-in account has no service provider profile — only providers can submit an application.");

        // Capability phải phù hợp service_kind của bài đăng (designer/constructor/both).
        // Dùng chung luật với bộ lọc danh sách bài — xem ProviderCapability.
        if (!ProviderCapability.CanApplyTo(provider.Capability, post.ServiceKind))
            throw new InvalidOperationException(
                $"ServiceProviderProfile capability '{provider.Capability}' does not match the post service kind '{post.ServiceKind}'.");

        var alreadyApplied = await _repository.CountAsync(
            a => a.PostId == post.Id && a.ServiceProviderProfileId == provider.Id && a.Status != ApplicationStatus.rejected) > 0;
        if (alreadyApplied)
            throw new InvalidOperationException("This ServiceProviderProfile has already applied to this post.");

        // Chỗ của dự án đã có người giữ thì hồ sơ nộp vào cũng vô nghĩa — chặn ngay từ đây thay vì
        // để provider chờ rồi bị từ chối ở bước accept. Xem ProjectSlotRules.
        await EnsureProjectSlotFreeAsync(
            post.ProjectShopOwnerId, post.ServiceKind,
            $"apply to a post with scope '{post.ServiceKind}'");

        var application = new Apply
        {
            PostId = post.Id,
            ServiceProviderProfileId = provider.Id,
            Proposal = request.Proposal,
            EstimatedDurationDays = request.EstimatedDurationDays,
            Status = ApplicationStatus.pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(application);
        await _unitOfWork.CommitAsync();

        // Thông báo cho OWNER: có provider vừa ứng tuyển vào bài đăng của họ.
        await _notificationService.NotifyApplicationReceivedAsync(application.Id);

        application.Post = post;
        application.ServiceProviderProfile = provider;
        return ApplyResponse.From(application);
    }

    public async Task<ApplyResponse> UpdateProposalAsync(Guid accountId, Guid id, UpdateApplyRequest request)
    {
        await EnsureApplicantAsync(accountId, id, "edit its proposal");

        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"No application found with id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"The application is in status '{application.Status}'; it can only be edited while pending.");

        if (request.ClearEstimatedDuration && request.EstimatedDurationDays.HasValue)
            throw new ArgumentException(
                "Send either an estimated duration or the request to clear it, not both.");

        if (request.Proposal != null) application.Proposal = request.Proposal;

        // Đây là partial update nên null = "đừng đụng tới"; xoá phải là ý định tường minh.
        if (request.ClearEstimatedDuration) application.EstimatedDurationDays = null;
        else if (request.EstimatedDurationDays.HasValue) application.EstimatedDurationDays = request.EstimatedDurationDays;

        application.UpdatedAt = DateTime.UtcNow;
        _repository.Update(application);
        await _unitOfWork.CommitAsync();

        return ApplyResponse.From(application);
    }

    public async Task<ProjectWorkingResponse> AcceptAsync(Guid accountId, Guid id)
    {
        await EnsurePostOwnerAsync(accountId, id, "accept it");

        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).ThenInclude(p => p.ProjectShopOwner).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"No application found with id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"The application is in status '{application.Status}'; it can only be accepted while pending.");

        var post = application.Post;
        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"The post is in status '{post.Status}'; the application cannot be accepted.");

        // Bài có pha thiết kế thì phải khảo sát thực tế rồi mới chốt được provider.
        await EnsureSurveySubmittedAsync(application.Id, post.ServiceKind);

        // Kiểm lại chỗ NGAY TRƯỚC khi tạo engagement, không tin vào lần check lúc nộp hồ sơ:
        // giữa hai thời điểm đó owner có thể đã mời trực tiếp một provider khác, hoặc đã accept
        // một hồ sơ ở bài đăng khác cùng dự án.
        await EnsureProjectSlotFreeAsync(
            post.ProjectShopOwnerId, post.ServiceKind,
            $"accept application #{application.Id} with scope '{post.ServiceKind}'");

        var now = DateTime.UtcNow;

        // 1. Chấp nhận hồ sơ này
        application.Status = ApplicationStatus.accepted;
        application.UpdatedAt = now;
        _repository.Update(application);

        // 2. Tạo engagement (đường marketplace: application_id có giá trị)
        var engagement = new ProjectWorking
        {
            ProjectShopOwnerId = post.ProjectShopOwnerId,
            ServiceProviderProfileId = application.ServiceProviderProfileId,
            ApplyId = application.Id,
            ContractType = post.ServiceKind,
            Status = ProviderStatus.accepted,
            RequestMessage = $"Accepted application #{application.Id} for post \"{post.Title}\".",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _unitOfWork.GetRepository<ProjectWorking>().InsertAsync(engagement);

        // 3. Đóng bài đăng
        post.Status = PostStatus.closed;
        post.UpdatedAt = now;
        _unitOfWork.GetRepository<Post>().Update(post);

        // 4. Từ chối các hồ sơ pending còn lại của bài đăng
        var otherPending = await _repository.GetListAsync(
            predicate: a => a.PostId == post.Id && a.Id != application.Id && a.Status == ApplicationStatus.pending);
        foreach (var other in otherPending)
        {
            other.Status = ApplicationStatus.rejected;
            other.UpdatedAt = now;
        }
        _repository.UpdateRange(otherPending);

        // 5. Chỗ vừa bị lấp → đóng nốt các BÀI ĐĂNG KHÁC của dự án đụng vào chỗ đó (owner có thể
        // đăng nhiều bài cho cùng một phạm vi) và từ chối hồ sơ đang chờ ở đó. Loại trừ post hiện
        // tại vì nó đã được xử lý ngay trên graph phía trên.
        var coveredApplicationIds = await ProjectSlotClosure.CloseCoveredPostsAsync(
            _unitOfWork, post.ProjectShopOwnerId, post.ServiceKind, excludePostId: post.Id);

        // SaveChanges chạy trong một transaction — 5 bước trên là atomic.
        await _unitOfWork.CommitAsync();

        // Thông báo cho provider được chấp nhận + các provider bị từ chối tự động.
        await _notificationService.NotifyApplicationDecisionAsync(application.Id, accepted: true);
        foreach (var other in otherPending)
            await _notificationService.NotifyApplicationDecisionAsync(other.Id, accepted: false);
        foreach (var applicationId in coveredApplicationIds)
            await _notificationService.NotifyApplicationDecisionAsync(applicationId, accepted: false);

        engagement.ProjectShopOwner = post.ProjectShopOwner;
        engagement.ServiceProviderProfile = application.ServiceProviderProfile;
        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ApplyResponse> RejectAsync(Guid accountId, Guid id)
    {
        await EnsurePostOwnerAsync(accountId, id, "reject it");

        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"No application found with id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"The application is in status '{application.Status}'; it can only be rejected while pending.");

        application.Status = ApplicationStatus.rejected;
        application.UpdatedAt = DateTime.UtcNow;
        _repository.Update(application);
        await _unitOfWork.CommitAsync();

        // Thông báo cho provider: hồ sơ bị từ chối.
        await _notificationService.NotifyApplicationDecisionAsync(application.Id, accepted: false);

        return ApplyResponse.From(application);
    }

    /// <summary>
    /// Chặn khi chỗ (design / construction) của dự án đã có engagement đang hoạt động giữ.
    /// Dùng chung cho lúc nộp hồ sơ và lúc owner chấp nhận hồ sơ.
    /// </summary>
    private async Task EnsureProjectSlotFreeAsync(
        Guid projectShopOwnerId, ServiceKind wanted, string action)
    {
        var activeKinds = await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            selector: e => e.ContractType,
            predicate: e => e.ProjectShopOwnerId == projectShopOwnerId
                            && ProjectSlotRules.OccupyingStatuses.Contains(e.Status));

        ProjectSlotRules.EnsureSlotFree(activeKinds, wanted, action);
    }

    public async Task WithdrawAsync(Guid accountId, Guid id)
    {
        await EnsureApplicantAsync(accountId, id, "withdraw it");

        var application = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No application found with id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"The application is in status '{application.Status}'; it can only be withdrawn while pending.");

        _repository.Delete(application);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Hai đầu account của một hồ sơ ứng tuyển. Dùng projection lấy đúng 2 cột thay vì nạp cả
    /// graph — cùng cách <c>CommentService.LoadApplyPartiesAsync</c> và
    /// <c>EngagementAuthorization</c> đang làm.
    /// </summary>
    private sealed record ApplyParties(Guid OwnerAccountId, Guid ProviderAccountId);

    private async Task<ApplyParties> LoadPartiesAsync(Guid applyId) =>
        (await _unitOfWork.GetRepository<Apply>().GetListAsync(
            selector: a => new ApplyParties(
                a.Post.ProjectShopOwner.Owner.AccountId,
                a.ServiceProviderProfile.AccountId),
            predicate: a => a.Id == applyId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"No application found with id {applyId}.");

    /// <summary>Chỉ chủ CỦA BÀI ĐĂNG (hoặc admin) — dùng cho accept/reject.</summary>
    private async Task EnsurePostOwnerAsync(Guid accountId, Guid applyId, string action)
    {
        var parties = await LoadPartiesAsync(applyId);
        if (parties.OwnerAccountId == accountId) return;
        if (await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId)) return;

        throw new UnauthorizedAccessException(
            $"Only the owner of the post this application was submitted to may {action}.");
    }

    /// <summary>Chỉ provider ĐÃ NỘP hồ sơ đó (hoặc admin) — dùng cho sửa proposal/rút hồ sơ.</summary>
    private async Task EnsureApplicantAsync(Guid accountId, Guid applyId, string action)
    {
        var parties = await LoadPartiesAsync(applyId);
        if (parties.ProviderAccountId == accountId) return;
        if (await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId)) return;

        throw new UnauthorizedAccessException(
            $"Only the provider who submitted this application may {action}.");
    }

    /// <summary>Một trong hai bên (hoặc admin) — dùng cho đọc chi tiết.</summary>
    private async Task EnsurePartyAsync(Guid accountId, Guid applyId)
    {
        var parties = await LoadPartiesAsync(applyId);
        if (parties.OwnerAccountId == accountId || parties.ProviderAccountId == accountId) return;
        if (await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId)) return;

        throw new UnauthorizedAccessException(
            "This application belongs to another provider and another post — you cannot read it.");
    }
}
