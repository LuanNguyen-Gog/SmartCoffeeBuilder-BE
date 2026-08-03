using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;

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

    public async Task<PaginationResponse<ProjectShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? ownerId = null)
    {
        var query = _repository
            .GetQueryable(
                p => p.DeletedAt == null && (ownerId == null || p.OwnerId == ownerId),
                include: q => q.Include(p => p.ProjectWorkings).ThenInclude(pp => pp.ServiceProviderProfile)
                                .Include(p => p.Owner)
                                .Include(p => p.Posts))
            .OrderByDescending(p => p.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectShopOwnerResponse>(
            paged.Items.Select(ProjectShopOwnerResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectShopOwnerResponse> GetByIdAsync(long id)
    {
        var project = await _repository.SingleOrDefaultAsync(
                predicate: p => p.Id == id && p.DeletedAt == null,
                include: q => q.Include(p => p.ProjectWorkings).ThenInclude(pp => pp.ServiceProviderProfile)
                                .Include(p => p.Owner)
                                .Include(p => p.Posts))
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {id}.");

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> CreateAsync(CreateProjectShopOwnerRequest request)
    {
        var owner = await _unitOfWork.GetRepository<ShopOwner>()
            .SingleOrDefaultAsync(predicate: s => s.Id == request.OwnerId)
            ?? throw new KeyNotFoundException($"Không tìm thấy shop owner với id {request.OwnerId}.");

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

        // Dự án tạo trước khi có auto-advance ở ContractService có thể còn kẹt 'briefed' dù đã ký
        // hợp đồng — tự nâng lên in_progress để dữ liệu cũ không bị chặn oan.
        if (project.Status == ProjectStatus.briefed)
        {
            var hasSignedContract = await _unitOfWork.GetRepository<Contract>().CountAsync(
                c => c.ProjectWorking.ProjectShopOwnerId == project.Id && c.Status == ContractStatus.confirmed) > 0;
            if (hasSignedContract) project.Status = ProjectStatus.in_progress;
        }

        EnsureTransition(project.Status, ProjectStatus.completed);

        var engagementRepo = _unitOfWork.GetRepository<ProjectWorking>();

        // Không cho đóng khi còn hợp tác dang dở — owner phải nghiệm thu/huỷ từng engagement trước.
        var openCount = await engagementRepo.CountAsync(
            e => e.ProjectShopOwnerId == project.Id && OpenEngagementStatuses.Contains(e.Status));
        if (openCount > 0)
            throw new InvalidOperationException(
                $"Còn {openCount} engagement chưa đóng (requested/accepted) — nghiệm thu hoặc huỷ ngang từng provider trước khi đóng dự án.");

        // Phải có ít nhất một provider được nghiệm thu, tránh "đóng" một dự án chưa chạy gì.
        var completedEngagements = await engagementRepo.GetListAsync(
            predicate: e => e.ProjectShopOwnerId == project.Id && e.Status == ProviderStatus.completed);
        if (completedEngagements.Count == 0)
            throw new InvalidOperationException(
                "Dự án chưa có engagement nào được nghiệm thu ('completed') — chưa thể đóng dự án.");

        await CloseOpenPostsAsync(project.Id);

        project.Status = ProjectStatus.completed;
        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);

        // Một SaveChanges → đóng post và đổi trạng thái dự án là atomic.
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho các provider đã tham gia dự án.
        await _notificationService.NotifyProjectClosedAsync(
            project.Id, cancelled: false, completedEngagements.Select(e => e.Id).ToList());

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task<ProjectShopOwnerResponse> CancelAsync(long accountId, long id)
    {
        var project = await LoadForActionAsync(id);
        await EnsureOwnerAsync(accountId, project, "huỷ dự án");

        EnsureTransition(project.Status, ProjectStatus.cancelled);

        // Huỷ dự án thì mọi hợp tác đang mở chấm dứt theo, đúng state machine của engagement:
        // requested → rejected (chưa nhận việc), accepted → terminated (đang chạy thì huỷ ngang).
        var engagementRepo = _unitOfWork.GetRepository<ProjectWorking>();
        var openEngagements = await engagementRepo.GetListAsync(
            predicate: e => e.ProjectShopOwnerId == project.Id && OpenEngagementStatuses.Contains(e.Status));

        foreach (var engagement in openEngagements)
        {
            engagement.Status = engagement.Status == ProviderStatus.requested
                ? ProviderStatus.rejected
                : ProviderStatus.terminated;
            engagement.CompletionRequestedAt = null;
            engagement.UpdatedAt = DateTime.UtcNow;
        }
        engagementRepo.UpdateRange(openEngagements);

        await CloseOpenPostsAsync(project.Id);

        project.Status = ProjectStatus.cancelled;
        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);

        // Một SaveChanges → đóng engagement, đóng post, đổi trạng thái dự án là atomic.
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho đúng các provider vừa bị đóng hợp tác theo.
        await _notificationService.NotifyProjectClosedAsync(
            project.Id, cancelled: true, openEngagements.Select(e => e.Id).ToList());

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

        project.DeletedAt = DateTime.UtcNow;
        _repository.Update(project);
        await _unitOfWork.CommitAsync();
    }

    // ──────────────────────────────────────────────────────────────

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

    /// <summary>Đóng dự án thì không nhận hồ sơ nữa — post còn 'open' chuyển sang 'closed'.</summary>
    private async Task CloseOpenPostsAsync(long projectId)
    {
        var postRepo = _unitOfWork.GetRepository<Post>();
        var openPosts = await postRepo.GetListAsync(
            predicate: p => p.ProjectShopOwnerId == projectId && p.Status == PostStatus.open);

        if (openPosts.Count == 0) return;

        foreach (var post in openPosts)
        {
            post.Status = PostStatus.closed;
            post.UpdatedAt = DateTime.UtcNow;
        }
        postRepo.UpdateRange(openPosts);
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

        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        if (account?.Role == AccountRole.admin) return;

        throw new UnauthorizedAccessException($"Chỉ chủ dự án mới được {action}.");
    }
}
