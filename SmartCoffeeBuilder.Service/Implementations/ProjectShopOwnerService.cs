using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Vòng đời dự án của owner: briefed → in_progress → completed, và briefed/in_progress → cancelled.
/// `in_progress` KHÔNG set tay — tự chuyển khi hợp đồng đầu tiên của dự án được ký
/// (ContractService.ConfirmOtpAsync). `completed` là bước đóng dự án cuối cùng của owner,
/// chỉ mở khi mọi engagement con đã đóng.
/// </summary>
public class ProjectShopOwnerService : IProjectShopOwnerService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectShopOwner> _repository;
    private readonly INotificationService _notificationService;

    // Engagement còn "mở" — chặn đóng dự án, và bị đóng theo khi owner huỷ dự án.
    private static readonly ProviderStatus[] OpenEngagementStatuses =
    [
        ProviderStatus.requested, ProviderStatus.accepted
    ];

    public ProjectShopOwnerService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectShopOwner>();
        _notificationService = notificationService;
    }

    public async Task<PaginationResponse<ProjectShopOwnerResponse>> GetAllAsync(
        long accountId, int pageNumber = 1, int pageSize = 10, long? ownerId = null)
    {
        // ownerId là bộ lọc do client tự khai nên không rào được gì. Quyền xem đi thẳng vào query
        // để TotalItems của phân trang cũng đúng theo góc nhìn người gọi (xem EnsureVisibleAsync).
        var isAdmin = await IsAdminAsync(accountId);

        var query = _repository
            .GetQueryable(
                p => p.DeletedAt == null
                     && (ownerId == null || p.OwnerId == ownerId)
                     && (isAdmin
                         || p.Owner.AccountId == accountId
                         || p.ProjectWorkings.Any(e => e.ServiceProviderProfile.AccountId == accountId
                                                       && e.Status != ProviderStatus.rejected
                                                       && e.Status != ProviderStatus.terminated)
                         || p.Posts.Any(post => post.Status == PostStatus.open)),
                include: q => q.Include(p => p.ProjectWorkings).ThenInclude(pp => pp.ServiceProviderProfile)
                                .Include(p => p.Owner)
                                .Include(p => p.Posts))
            .OrderByDescending(p => p.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectShopOwnerResponse>(
            paged.Items.Select(ProjectShopOwnerResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectShopOwnerResponse> GetByIdAsync(long accountId, long id)
    {
        var project = await LoadForActionAsync(id);

        await EnsureVisibleAsync(accountId, project);

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> CreateAsync(long accountId, CreateProjectShopOwnerRequest request)
    {
        var owner = await _unitOfWork.GetRepository<ShopOwner>()
            .SingleOrDefaultAsync(predicate: s => s.Id == request.OwnerId)
            ?? throw new KeyNotFoundException($"Không tìm thấy shop owner với id {request.OwnerId}.");

        // ownerId vẫn nhận từ body để giữ nguyên hợp đồng API, nhưng chỉ chấp nhận khi nó TRÙNG hồ
        // sơ chủ quán của chính tài khoản đang đăng nhập — client tự khai thì cột owner_id mất giá
        // trị đối chứng và ai cũng tạo được dự án đứng tên người khác.
        if (owner.AccountId != accountId && !await IsAdminAsync(accountId))
            throw new UnauthorizedAccessException(
                "Chỉ tạo được dự án cho hồ sơ chủ quán của chính tài khoản đang đăng nhập.");

        var project = new ProjectShopOwner
        {
            OwnerId = owner.Id,
            Name = request.Name,
            Address = request.Address,
            AreaM2 = request.AreaM2,
            Budget = request.Budget,
            Status = ProjectStatus.briefed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(project);
        await _unitOfWork.CommitAsync();

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> UpdateAsync(long accountId, long id, UpdateProjectShopOwnerRequest request)
    {
        var project = await LoadForActionAsync(id);
        await EnsureOwnerAsync(accountId, project, "sửa dự án");

        // Dự án đã đóng thì không sửa nội dung nữa.
        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled
            && (request.Name != null || request.Address != null || request.AreaM2.HasValue || request.Budget.HasValue))
            throw new InvalidOperationException(
                $"Dự án đang ở trạng thái '{project.Status}' — không sửa được thông tin nữa.");

        if (request.Name != null) project.Name = request.Name;
        if (request.Address != null) project.Address = request.Address;
        if (request.AreaM2.HasValue) project.AreaM2 = request.AreaM2.Value;
        if (request.Budget.HasValue) project.Budget = request.Budget.Value;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<ProjectStatus>(request.Status, ignoreCase: true, out var status))
                throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: briefed, in_progress, completed, cancelled.");

            if (status != project.Status)
            {
                // Đóng dự án đi kèm guard + dọn dẹp — bắt buộc qua endpoint chuyên dụng.
                if (status is ProjectStatus.completed or ProjectStatus.cancelled)
                    throw new InvalidOperationException(
                        $"Không đóng dự án qua PUT — dùng POST /api/project-shop-owners/{{id}}/{(status == ProjectStatus.completed ? "complete" : "cancel")}.");

                EnsureTransition(project.Status, status);
                project.Status = status;
            }
        }

        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);
        await _unitOfWork.CommitAsync();

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> CompleteAsync(long accountId, long id)
    {
        var project = await LoadForActionAsync(id);
        await EnsureOwnerAsync(accountId, project, "đóng dự án");

        var signedEngagementIds = await GetSignedEngagementIdsAsync(project.Id);

        // Dự án tạo trước khi có auto-advance ở ContractService có thể còn kẹt 'briefed' dù đã ký
        // hợp đồng — tự nâng lên in_progress để dữ liệu cũ không bị chặn oan. Chưa ký gì thì chưa
        // có ai làm gì cả: không nghiệm thu được, chỉ còn đường huỷ.
        if (project.Status == ProjectStatus.briefed)
        {
            if (signedEngagementIds.Count == 0)
                throw new InvalidOperationException(
                    "Dự án chưa có hợp đồng nào được ký nên chưa có gì để nghiệm thu — chỉ có thể " +
                    "huỷ dự án (POST /api/project-shop-owners/{id}/cancel).");

            project.Status = ProjectStatus.in_progress;
        }

        EnsureTransition(project.Status, ProjectStatus.completed);

        // Engagement/post lấy thẳng từ graph LoadForActionAsync đã nạp — query lại sẽ tạo
        // instance thứ hai của cùng một dòng (reads đều AsNoTracking) và làm hỏng change tracker.
        // Luật đóng dự án nằm ở ProjectClosureRules để noti nhắc-đóng-dự-án dùng chung đúng một bản.
        var blocker = ProjectClosureRules.FindBlocker(project.ProjectWorkings, signedEngagementIds);
        if (blocker != null) throw new InvalidOperationException(blocker);

        var completedEngagements = project.ProjectWorkings
            .Where(e => e.Status == ProviderStatus.completed)
            .ToList();

        var rejectedApplicationIds = await CloseOpenPostsAsync(project);

        project.Status = ProjectStatus.completed;
        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);

        // Một SaveChanges → đóng post, từ chối hồ sơ và đổi trạng thái dự án là atomic.
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho các provider đã tham gia dự án.
        await _notificationService.NotifyProjectClosedAsync(
            project.Id, cancelled: false, completedEngagements.Select(e => e.Id).ToList());

        // ...và cho các provider có hồ sơ bị đóng theo, nếu không họ chờ mãi ở 'pending'.
        await NotifyApplicationsRejectedAsync(rejectedApplicationIds);

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> CancelAsync(long accountId, long id)
    {
        var project = await LoadForActionAsync(id);
        await EnsureOwnerAsync(accountId, project, "huỷ dự án");

        EnsureTransition(project.Status, ProjectStatus.cancelled);

        // Huỷ dự án thì mọi hợp tác đang mở chấm dứt theo, đúng state machine của engagement:
        // requested → rejected (chưa nhận việc), accepted → terminated (đang chạy thì huỷ ngang).
        // Lấy từ graph đã nạp, không query lại (xem ghi chú ở CompleteAsync).
        var openEngagements = project.ProjectWorkings
            .Where(e => OpenEngagementStatuses.Contains(e.Status))
            .ToList();

        // ...nhưng CHỈ với hợp tác chưa ký hợp đồng. Hợp đồng đã ký chỉ chấm dứt được khi hai bên
        // đồng thuận (ProjectWorkingService.RequestTermination → RespondTermination); nếu huỷ dự án
        // đóng luôn được chúng thì owner có cửa sau để đơn phương xé hợp đồng đã ký.
        var signedEngagementIds = await GetSignedEngagementIdsAsync(project.Id);
        var signedOpen = openEngagements.Where(e => signedEngagementIds.Contains(e.Id)).ToList();
        if (signedOpen.Count > 0)
        {
            var scopes = string.Join(" và ", signedOpen
                .Select(e => ProjectSlotRules.ScopeLabel(e.ContractType))
                .Distinct());
            throw new InvalidOperationException(
                $"Còn {signedOpen.Count} hợp tác đã ký hợp đồng (phần {scopes}) — hợp đồng đã ký chỉ " +
                "chấm dứt được khi cả hai bên đồng ý. Gửi đề nghị huỷ ngang " +
                "(POST /api/project-workings/{id}/termination-request) và đợi bên kia phản hồi, hoặc " +
                "nghiệm thu nếu đã xong, rồi mới huỷ dự án.");
        }

        var now = DateTime.UtcNow;
        foreach (var engagement in openEngagements)
        {
            var terminated = engagement.Status == ProviderStatus.accepted;
            engagement.Status = terminated ? ProviderStatus.terminated : ProviderStatus.rejected;

            // Khớp vết với ProjectWorkingService.CloseAsTerminated: 'terminated' phải có
            // terminated_at (thiếu nó thì báo cáo/FE đọc ra engagement huỷ mà không có mốc huỷ),
            // và đề nghị huỷ ngang đang treo hết ý nghĩa khi cả dự án đã huỷ — để lại thì FE vẫn
            // hiện "chờ bạn phản hồi đề nghị huỷ ngang" trên một hợp tác đã đóng.
            if (terminated) engagement.TerminatedAt = now;
            engagement.TerminationRequestedAt = null;
            engagement.TerminationRequestedBy = null;
            engagement.TerminationRequestNote = null;
            engagement.CompletionRequestedAt = null;
            engagement.CompletionRequestNote = null;
            engagement.UpdatedAt = now;
        }
        _unitOfWork.GetRepository<ProjectWorking>().UpdateRange(openEngagements);

        var rejectedApplicationIds = await CloseOpenPostsAsync(project);

        project.Status = ProjectStatus.cancelled;
        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);

        // Một SaveChanges → đóng engagement, đóng post, từ chối hồ sơ, đổi trạng thái dự án là atomic.
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho đúng các provider vừa bị đóng hợp tác theo.
        await _notificationService.NotifyProjectClosedAsync(
            project.Id, cancelled: true, openEngagements.Select(e => e.Id).ToList());

        // ...và cho các provider có hồ sơ bị đóng theo, nếu không họ chờ mãi ở 'pending'.
        await NotifyApplicationsRejectedAsync(rejectedApplicationIds);

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task DeleteAsync(long accountId, long id)
    {
        var project = await LoadForActionAsync(id);
        await EnsureOwnerAsync(accountId, project, "xoá dự án");

        var openCount = await _unitOfWork.GetRepository<ProjectWorking>().CountAsync(
            e => e.ProjectShopOwnerId == project.Id && OpenEngagementStatuses.Contains(e.Status));
        if (openCount > 0)
            throw new InvalidOperationException(
                $"Còn {openCount} engagement chưa đóng — huỷ dự án (POST /cancel) trước khi xoá.");

        if (project.Status == ProjectStatus.in_progress)
            throw new InvalidOperationException(
                "Dự án đang 'in_progress' — nghiệm thu (POST /complete) hoặc huỷ (POST /cancel) trước khi xoá.");

        project.DeletedAt = DateTime.UtcNow;
        _repository.Update(project);
        await _unitOfWork.CommitAsync();
    }

    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Báo cho từng provider có hồ sơ bị từ chối theo khi dự án đóng/huỷ — dùng lại đúng noti
    /// 'application_rejected' của luồng owner từ chối hồ sơ. Best-effort như mọi noti khác.
    /// </summary>
    private async Task NotifyApplicationsRejectedAsync(IReadOnlyCollection<long> applicationIds)
    {
        foreach (var applicationId in applicationIds)
            await _notificationService.NotifyApplicationDecisionAsync(applicationId, accepted: false);
    }

    /// <summary>
    /// Id các engagement của dự án ĐÃ TỪNG ký hợp đồng. <c>confirmed</c> là trạng thái cuối của
    /// contract (<c>ContractService.EnsureTransition</c> không cho quay ngược) nên trạng thái hiện
    /// tại trả lời đúng câu hỏi "đã từng ký chưa".
    /// </summary>
    private async Task<IReadOnlySet<long>> GetSignedEngagementIdsAsync(long projectShopOwnerId)
    {
        var ids = await _unitOfWork.GetRepository<Contract>().GetListAsync(
            selector: c => c.ProjectWorkingId,
            predicate: c => c.ProjectWorking.ProjectShopOwnerId == projectShopOwnerId
                            && c.Status == ContractStatus.confirmed);

        return ids.ToHashSet();
    }

    /// <summary>
    /// Transition hợp lệ của dự án: briefed → in_progress | cancelled;
    /// in_progress → completed | cancelled. completed/cancelled là trạng thái cuối.
    /// </summary>
    private static void EnsureTransition(ProjectStatus current, ProjectStatus target)
    {
        var allowed = current switch
        {
            ProjectStatus.briefed => target is ProjectStatus.in_progress or ProjectStatus.cancelled,
            ProjectStatus.in_progress => target is ProjectStatus.completed or ProjectStatus.cancelled,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Không thể chuyển dự án từ '{current}' sang '{target}'.");
    }

    /// <summary>
    /// Đóng dự án thì không nhận hồ sơ nữa — post còn 'open' chuyển sang 'closed', và mọi hồ sơ
    /// còn 'pending' của các post đó chuyển sang 'rejected': bài đăng đã đóng vĩnh viễn nên để hồ sơ
    /// treo ở 'pending' là nói dối provider.
    /// Duyệt trên chính collection <c>project.Posts</c> đã nạp: response trả về phản ánh đúng
    /// trạng thái mới, và không sinh instance Post thứ hai cho cùng một dòng.
    /// KHÔNG commit — caller gộp chung một SaveChanges để đóng post/hồ sơ/dự án là atomic.
    /// </summary>
    /// <returns>Id các hồ sơ vừa bị từ chối theo — caller bắn noti SAU khi commit.</returns>
    private async Task<List<long>> CloseOpenPostsAsync(ProjectShopOwner project)
    {
        var openPosts = project.Posts.Where(p => p.Status == PostStatus.open).ToList();
        if (openPosts.Count == 0) return [];

        foreach (var post in openPosts)
        {
            post.Status = PostStatus.closed;
            post.UpdatedAt = DateTime.UtcNow;
        }
        _unitOfWork.GetRepository<Post>().UpdateRange(openPosts);

        // Hồ sơ không nằm trong graph của project nên query riêng — không đụng instance Post ở trên.
        var postIds = openPosts.Select(p => p.Id).ToList();
        var applyRepository = _unitOfWork.GetRepository<Apply>();
        var pendingApplications = await applyRepository.GetListAsync(
            predicate: a => postIds.Contains(a.PostId) && a.Status == ApplicationStatus.pending);
        if (pendingApplications.Count == 0) return [];

        foreach (var application in pendingApplications)
        {
            application.Status = ApplicationStatus.rejected;
            application.UpdatedAt = DateTime.UtcNow;
        }
        applyRepository.UpdateRange(pendingApplications);

        return pendingApplications.Select(a => a.Id).ToList();
    }

    // Include đủ như GetByIdAsync: response sau khi đóng/huỷ phải phản ánh luôn engagement và
    // post vừa bị đóng theo (cùng change tracker nên các thay đổi bên dưới hiện ra ở đây).
    private async Task<ProjectShopOwner> LoadForActionAsync(long id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id && p.DeletedAt == null,
            include: q => q.Include(p => p.ProjectWorkings).ThenInclude(pp => pp.ServiceProviderProfile)
                           .Include(p => p.Owner)
                           .Include(p => p.Posts))
        ?? throw new KeyNotFoundException($"Không tìm thấy project với id {id}.");

    /// <summary>
    /// Chỉ owner sở hữu dự án (hoặc admin) mới thao tác được vòng đời dự án.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không phải chủ dự án (HTTP 401).</exception>
    private async Task EnsureOwnerAsync(long accountId, ProjectShopOwner project, string action)
    {
        if (project.Owner?.AccountId == accountId) return;
        if (await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException($"Chỉ chủ dự án mới được {action}.");
    }

    /// <summary>
    /// Ai được ĐỌC một dự án: chủ dự án; provider có engagement còn hiệu lực (rejected/terminated
    /// thì hết quyền, khớp <c>ProjectWorkingService.EnsureEngagementViewable</c>); mọi provider khi
    /// dự án còn bài đăng 'open' — bài đăng là lời mời thầu công khai, không xem được dự án thì
    /// không nộp hồ sơ được; và admin.
    ///
    /// Kiểm tra trên graph đã nạp sẵn ở <see cref="LoadForActionAsync"/>, không query thêm.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Dự án riêng tư và người gọi không tham gia (HTTP 401).</exception>
    private async Task EnsureVisibleAsync(long accountId, ProjectShopOwner project)
    {
        if (project.Owner?.AccountId == accountId) return;
        if (project.Posts.Any(post => post.Status == PostStatus.open)) return;
        if (project.ProjectWorkings.Any(e => e.ServiceProviderProfile?.AccountId == accountId
                                             && e.Status is not (ProviderStatus.rejected or ProviderStatus.terminated)))
            return;
        if (await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            "Dự án này không mở thầu công khai và tài khoản đang đăng nhập không tham gia.");
    }

    private async Task<bool> IsAdminAsync(long accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }
}
