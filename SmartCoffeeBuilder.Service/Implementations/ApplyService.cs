using Microsoft.EntityFrameworkCore;
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
        int pageNumber = 1, int pageSize = 10,
        Guid? postId = null, Guid? serviceProviderProfileId = null, string? status = null)
    {
        ApplicationStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, accepted, rejected.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(
                a => a.ServiceProviderProfile.DeletedAt == null      // ẩn hồ sơ của provider đã xoá mềm
                     && a.Post.ProjectShopOwner.DeletedAt == null     // và của bài thuộc dự án đã xoá mềm
                     && (postId == null || a.PostId == postId)
                     && (serviceProviderProfileId == null || a.ServiceProviderProfileId == serviceProviderProfileId)
                     && (st == null || a.Status == st),
                include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            .OrderByDescending(a => a.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ApplyResponse>(
            paged.Items.Select(ApplyResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ApplyResponse> GetByIdAsync(Guid id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id
                            && a.ServiceProviderProfile.DeletedAt == null
                            && a.Post.ProjectShopOwner.DeletedAt == null,
            include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        return ApplyResponse.From(application);
    }

    public async Task<ApplyResponse> ApplyAsync(Guid accountId, CreateApplyRequest request)
    {
        var post = await _unitOfWork.GetRepository<Post>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.PostId)
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {request.PostId}.");

        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"Bài đăng đang ở trạng thái '{post.Status}', không nhận hồ sơ.");

        if (post.SubmissionDeadline.HasValue && post.SubmissionDeadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("Bài đăng đã quá hạn nộp hồ sơ.");

        // Hồ sơ provider lấy theo account đang đăng nhập — không nhận id từ client.
        var provider = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .SingleOrDefaultAsync(predicate: s => s.AccountId == accountId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException("Tài khoản đang đăng nhập không có hồ sơ service provider — chỉ provider mới nộp được hồ sơ.");

        // Capability phải phù hợp service_kind của bài đăng (designer/constructor/both).
        var capabilityMatches = provider.Capability == Capability.both
            || (post.ServiceKind == ServiceKind.design && provider.Capability == Capability.designer)
            || (post.ServiceKind == ServiceKind.construction && provider.Capability == Capability.constructor);
        if (!capabilityMatches)
            throw new InvalidOperationException(
                $"ServiceProviderProfile capability '{provider.Capability}' không phù hợp với bài đăng service kind '{post.ServiceKind}'.");

        var alreadyApplied = await _repository.CountAsync(
            a => a.PostId == post.Id && a.ServiceProviderProfileId == provider.Id && a.Status != ApplicationStatus.rejected) > 0;
        if (alreadyApplied)
            throw new InvalidOperationException("ServiceProviderProfile đã nộp hồ sơ cho bài đăng này.");

        // Chỗ của dự án đã có người giữ thì hồ sơ nộp vào cũng vô nghĩa — chặn ngay từ đây thay vì
        // để provider chờ rồi bị từ chối ở bước accept. Xem ProjectSlotRules.
        await EnsureProjectSlotFreeAsync(
            post.ProjectShopOwnerId, post.ServiceKind,
            $"nộp hồ sơ cho bài đăng phạm vi '{post.ServiceKind}'");

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

    public async Task<ApplyResponse> UpdateProposalAsync(Guid id, UpdateApplyRequest request)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Apply đang ở trạng thái '{application.Status}', chỉ sửa được khi pending.");

        if (request.Proposal != null) application.Proposal = request.Proposal;
        if (request.EstimatedDurationDays.HasValue) application.EstimatedDurationDays = request.EstimatedDurationDays;

        application.UpdatedAt = DateTime.UtcNow;
        _repository.Update(application);
        await _unitOfWork.CommitAsync();

        return ApplyResponse.From(application);
    }

    public async Task<ProjectWorkingResponse> AcceptAsync(Guid id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).ThenInclude(p => p.ProjectShopOwner).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Apply đang ở trạng thái '{application.Status}', chỉ chấp nhận được khi pending.");

        var post = application.Post;
        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"Bài đăng đang ở trạng thái '{post.Status}', không thể chấp nhận hồ sơ.");

        // Kiểm lại chỗ NGAY TRƯỚC khi tạo engagement, không tin vào lần check lúc nộp hồ sơ:
        // giữa hai thời điểm đó owner có thể đã mời trực tiếp một provider khác, hoặc đã accept
        // một hồ sơ ở bài đăng khác cùng dự án.
        await EnsureProjectSlotFreeAsync(
            post.ProjectShopOwnerId, post.ServiceKind,
            $"chấp nhận hồ sơ #{application.Id} với phạm vi '{post.ServiceKind}'");

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
            RequestMessage = $"Chấp nhận hồ sơ ứng tuyển #{application.Id} cho bài đăng \"{post.Title}\".",
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

    public async Task<ApplyResponse> RejectAsync(Guid id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Apply đang ở trạng thái '{application.Status}', chỉ từ chối được khi pending.");

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

    public async Task WithdrawAsync(Guid id)
    {
        var application = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Apply đang ở trạng thái '{application.Status}', chỉ rút được khi pending.");

        _repository.Delete(application);
        await _unitOfWork.CommitAsync();
    }
}
