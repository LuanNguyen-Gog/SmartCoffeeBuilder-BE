using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ProjectWorkingService : IProjectWorkingService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectWorking> _repository;
    private readonly INotificationService _notificationService;

    // Trạng thái coi là "đang hoạt động" — chặn thuê trùng, cho phép terminate.
    private static readonly ProviderStatus[] ActiveStatuses =
    [
        ProviderStatus.requested, ProviderStatus.accepted
    ];

    public ProjectWorkingService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectWorking>();
        _notificationService = notificationService;
    }

    public async Task<PaginationResponse<ProjectWorkingResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectShopOwnerId = null, long? serviceProviderProfileId = null, string? status = null)
    {
        ProviderStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProviderStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(
                e => (projectShopOwnerId == null || e.ProjectShopOwnerId == projectShopOwnerId)
                     && (serviceProviderProfileId == null || e.ServiceProviderProfileId == serviceProviderProfileId)
                     && (st == null || e.Status == st),
                include: q => q.Include(e => e.ProjectShopOwner)
                               .Include(e => e.ServiceProviderProfile)
                               .Include(e => e.Contracts))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectWorkingResponse>(
            paged.Items.Select(ProjectWorkingResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectWorkingResponse> GetByIdAsync(long id)
    {
        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ProjectShopOwner)
                           .Include(e => e.ServiceProviderProfile)
                           .Include(e => e.Contracts))
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ProjectWorkingResponse> CreateDirectRequestAsync(CreateProjectWorkingRequest request)
    {
        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.ProjectShopOwnerId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectShopOwnerId}.");

        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled)
            throw new InvalidOperationException($"ProjectShopOwner đang ở trạng thái '{project.Status}', không thể thuê provider.");

        var provider = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .SingleOrDefaultAsync(predicate: s => s.Id == request.ServiceProviderProfileId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {request.ServiceProviderProfileId}.");

        if (!Enum.TryParse<ServiceKind>(request.ContractType, ignoreCase: true, out var contractType))
            throw new ArgumentException($"ContractType '{request.ContractType}' không hợp lệ. Cho phép: design, construction, both.");

        var capabilityMatches = provider.Capability == Capability.both
            || (contractType == ServiceKind.design && provider.Capability == Capability.designer)
            || (contractType == ServiceKind.construction && provider.Capability == Capability.constructor);
        if (!capabilityMatches)
            throw new InvalidOperationException(
                $"ServiceProviderProfile capability '{provider.Capability}' không phù hợp với contract type '{contractType}'.");

        var duplicated = await _repository.CountAsync(
            e => e.ProjectShopOwnerId == project.Id && e.ServiceProviderProfileId == provider.Id && ActiveStatuses.Contains(e.Status)) > 0;
        if (duplicated)
            throw new InvalidOperationException("ServiceProviderProfile này đã có engagement đang hoạt động với project.");

        var engagement = new ProjectWorking
        {
            ProjectShopOwnerId = project.Id,
            ServiceProviderProfileId = provider.Id,
            ApplyId = null,
            ContractType = contractType,
            Status = ProviderStatus.requested,
            RequestMessage = request.RequestMessage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho PROVIDER biết họ vừa được mời hợp tác trực tiếp.
        await _notificationService.NotifyEngagementInvitedAsync(engagement.Id);

        engagement.ProjectShopOwner = project;
        engagement.ServiceProviderProfile = provider;
        return ProjectWorkingResponse.From(engagement);
    }

    public Task<ProjectWorkingResponse> AcceptAsync(long accountId, long id) =>
        TransitionAsync(accountId, id, ProviderStatus.accepted);

    public Task<ProjectWorkingResponse> RejectAsync(long accountId, long id) =>
        TransitionAsync(accountId, id, ProviderStatus.rejected);

    public Task<ProjectWorkingResponse> CompleteAsync(long accountId, long id) =>
        TransitionAsync(accountId, id, ProviderStatus.completed);

    public Task<ProjectWorkingResponse> TerminateAsync(long accountId, long id) =>
        TransitionAsync(accountId, id, ProviderStatus.terminated);

    public async Task<ProjectWorkingResponse> UpdateStatusAsync(
        long accountId, long id, UpdateProjectWorkingStatusRequest request)
    {
        if (!Enum.TryParse<ProviderStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.");

        // Endpoint tổng chỉ là cửa vào — mọi kiểm tra nằm trong TransitionAsync để một luật duy nhất.
        return await TransitionAsync(accountId, id, target);
    }

    public async Task<ProjectWorkingResponse> RequestCompletionAsync(
        long accountId, long id, RequestEngagementCompletionRequest request)
    {
        var engagement = await LoadForActionAsync(id);

        var actor = await ResolveActorAsync(accountId, engagement);
        EnsureActor(actor, "báo hoàn thành phần việc", EngagementActor.Provider);

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ báo hoàn thành khi engagement 'accepted'.");

        await EnsureConfirmedContractAsync(engagement);
        await EnsureDeliverablesReadyAsync(engagement, "chưa thể báo hoàn thành");

        // Gửi lại được (cập nhật ghi chú + mốc thời gian) khi owner chưa nghiệm thu.
        engagement.CompletionRequestedAt = DateTime.UtcNow;
        engagement.CompletionRequestNote = request.Note;
        engagement.UpdatedAt = DateTime.UtcNow;

        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu (giống ApplyService) — noti dùng chung UnitOfWork nên không commit đè lên nghiệp vụ.
        await _notificationService.NotifyEngagementCompletionRequestedAsync(engagement.Id);

        return ProjectWorkingResponse.From(engagement);
    }

    // Một cửa duy nhất cho mọi đổi trạng thái quan hệ: quyền → transition → guard nghiệp vụ.
    private async Task<ProjectWorkingResponse> TransitionAsync(long accountId, long id, ProviderStatus target)
    {
        var engagement = await LoadForActionAsync(id);

        var actor = await ResolveActorAsync(accountId, engagement);
        switch (target)
        {
            // Nhận/từ chối lời mời là quyết định của provider được mời.
            case ProviderStatus.accepted:
            case ProviderStatus.rejected:
                EnsureActor(actor, $"chuyển engagement sang '{target}'", EngagementActor.Provider);
                break;

            // Nghiệm thu là hành động của owner (v5) — mở khoá review.
            case ProviderStatus.completed:
                EnsureActor(actor, "nghiệm thu engagement", EngagementActor.Owner);
                break;

            // Huỷ ngang: cả hai bên đều có thể dừng hợp tác đang chạy.
            case ProviderStatus.terminated:
                EnsureActor(actor, "huỷ ngang engagement", EngagementActor.Owner, EngagementActor.Provider);
                break;

            default:
                throw new ArgumentException($"Status '{target}' không phải trạng thái đích hợp lệ.");
        }

        ValidateTransition(engagement, target);

        if (target == ProviderStatus.completed)
        {
            // Nghiệm thu: engagement phải đã chạy thật (có contract confirmed) mới completed được.
            await EnsureConfirmedContractAsync(engagement);

            // Owner nghiệm thu khi provider ĐÃ báo xong, HOẶC khi sản phẩm bàn giao thực sự đã xong
            // (design approved / mọi milestone completed) — không cho nghiệm thu một engagement trống.
            if (engagement.CompletionRequestedAt == null)
                await EnsureDeliverablesReadyAsync(
                    engagement, "chưa thể nghiệm thu (hoặc chờ nhà cung cấp bấm báo hoàn thành)");
        }

        engagement.Status = target;
        // Huỷ ngang thì yêu cầu nghiệm thu đang treo không còn ý nghĩa.
        if (target == ProviderStatus.terminated) engagement.CompletionRequestedAt = null;
        engagement.UpdatedAt = DateTime.UtcNow;

        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho bên còn lại biết kết quả.
        // requested → accepted/rejected chỉ xảy ra ở lời mời trực tiếp (direct-hire) do provider phản hồi;
        // báo cho OWNER (người gửi lời mời) biết provider đã nhận hay từ chối.
        if (target == ProviderStatus.accepted || target == ProviderStatus.rejected)
            await _notificationService.NotifyEngagementInviteDecisionAsync(
                engagement.Id, accepted: target == ProviderStatus.accepted);
        else if (target == ProviderStatus.completed)
            await _notificationService.NotifyEngagementCompletedAsync(engagement.Id);
        else if (target == ProviderStatus.terminated)
            await _notificationService.NotifyEngagementTerminatedAsync(
                engagement.Id, terminatedByOwner: actor != EngagementActor.Provider);

        // Đóng một engagement có thể là mảnh ghép cuối của cả dự án — nhắc owner bấm đóng dự án,
        // nếu không dự án nằm mãi ở 'in_progress' dù mọi hợp tác đã xong. Điều kiện đủ do
        // NotificationService tự xét (trùng guard của ProjectShopOwnerService.CompleteAsync).
        if (target is ProviderStatus.completed or ProviderStatus.terminated)
            await _notificationService.NotifyProjectReadyToCloseAsync(engagement.ProjectShopOwnerId);

        return ProjectWorkingResponse.From(engagement);
    }

    private async Task<ProjectWorking> LoadForActionAsync(long id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ProjectShopOwner).ThenInclude(p => p.Owner)
                           .Include(e => e.ServiceProviderProfile)
                           .Include(e => e.Contracts))
        ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

    private async Task EnsureConfirmedContractAsync(ProjectWorking engagement)
    {
        var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!hasConfirmedContract)
            throw new InvalidOperationException(
                "Engagement chưa có contract 'confirmed' — chưa bắt đầu thực hiện nên không thể nghiệm thu.");
    }

    /// <summary>
    /// Sản phẩm bàn giao đã thực sự xong theo contract_type:
    /// design → có ít nhất 1 bản 'approved' (bản approved luôn đã qua submit nên chắc chắn có file);
    /// construction → có milestone và mọi milestone ở 'completed'.
    /// Dùng cho cả hai phía: provider xin nghiệm thu, và owner nghiệm thu khi provider chưa kịp báo.
    /// </summary>
    /// <param name="blockedAction">Vế sau của thông báo lỗi, mô tả việc đang bị chặn.</param>
    private async Task EnsureDeliverablesReadyAsync(ProjectWorking engagement, string blockedAction)
    {
        if (engagement.ContractType is ServiceKind.design or ServiceKind.both)
        {
            var designRepo = _unitOfWork.GetRepository<Design>();
            var approved = await designRepo.CountAsync(
                d => d.ProjectWorkingId == engagement.Id && d.Status == DesignStatus.approved);
            if (approved == 0)
                throw new InvalidOperationException(
                    $"Chưa có bản design nào được duyệt ('approved') — {blockedAction}.");
        }

        if (engagement.ContractType is ServiceKind.construction or ServiceKind.both)
        {
            var itemRepo = _unitOfWork.GetRepository<ConstructionItem>();
            var total = await itemRepo.CountAsync(i => i.ProjectWorkingId == engagement.Id);
            if (total == 0)
                throw new InvalidOperationException(
                    $"Chưa có hạng mục thi công nào — {blockedAction}.");

            var unfinished = await itemRepo.CountAsync(
                i => i.ProjectWorkingId == engagement.Id && i.Status != ItemStatus.completed);
            if (unfinished > 0)
                throw new InvalidOperationException(
                    $"Còn {unfinished} hạng mục thi công chưa 'completed' — {blockedAction}.");
        }
    }

    public async Task<DesignBriefResponse> GetBriefAsync(long id)
    {
        var engagement = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        EnsureEngagementViewable(engagement);

        var brief = await _unitOfWork.GetRepository<DesignBrief>().SingleOrDefaultAsync(
            predicate: b => b.ProjectShopOwnerId == engagement.ProjectShopOwnerId,
            orderBy: q => q.OrderByDescending(b => b.CreatedAt))
            ?? throw new KeyNotFoundException("ProjectShopOwner chưa có brief — owner cần tạo brief trước.");

        return DesignBriefResponse.From(brief);
    }

    public async Task<EngagementOverviewResponse> GetOverviewAsync(long id)
    {
        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ProjectShopOwner))
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        EnsureEngagementViewable(engagement);

        var overview = new EngagementOverviewResponse
        {
            ProjectWorkingId = engagement.Id,
            ContractType = engagement.ContractType.ToString(),
            Status = engagement.Status.ToString(),
            ProjectShopOwner = OverviewProjectSummary.From(engagement.ProjectShopOwner)
        };

        if (engagement.ContractType is ServiceKind.design or ServiceKind.both)
        {
            // Bên design: brief + các kết quả AI đã hoàn tất (bước AI của owner).
            var brief = await _unitOfWork.GetRepository<DesignBrief>().SingleOrDefaultAsync(
                predicate: b => b.ProjectShopOwnerId == engagement.ProjectShopOwnerId,
                orderBy: q => q.OrderByDescending(b => b.CreatedAt));
            overview.Brief = brief != null ? DesignBriefResponse.From(brief) : null;

            var recommendations = await _unitOfWork.GetRepository<AiRecommendation>().GetListAsync(
                predicate: r => r.Brief.ProjectShopOwnerId == engagement.ProjectShopOwnerId && r.State == "completed",
                orderBy: q => q.OrderByDescending(r => r.CreatedAt));
            overview.AiRecommendations = recommendations.Select(AiRecommendationResponse.From).ToList();
        }
        else
        {
            // Bên construction (không kiêm design): xem bản vẽ đã 'approved' của bên design.
            var designs = await _unitOfWork.GetRepository<Design>().GetListAsync(
                predicate: d => d.ProjectWorking.ProjectShopOwnerId == engagement.ProjectShopOwnerId
                                && d.Status == DesignStatus.approved,
                orderBy: q => q.OrderByDescending(d => d.UpdatedAt),
                include: q => q.Include(d => d.DesignImages));
            overview.ApprovedDesigns = designs.Select(DesignResponse.From).ToList();
        }

        return overview;
    }

    // Brief/overview mở từ lúc được mời (requested) để provider quyết định nhận việc;
    // engagement đã rejected/terminated thì không còn quyền xem.
    private static void EnsureEngagementViewable(ProjectWorking engagement)
    {
        if (engagement.Status is ProviderStatus.rejected or ProviderStatus.terminated)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — không còn quyền xem thông tin dự án.");
    }

    // v5 — provider_status là trạng thái QUAN HỆ, không phải tiến độ:
    // requested → accepted | rejected; accepted → completed | terminated.
    // Tiến độ (designing/constructing…) là derived từ contract_type + design/construction_item con.
    private static void ValidateTransition(ProjectWorking engagement, ProviderStatus target)
    {
        var current = engagement.Status;

        var allowed = current switch
        {
            ProviderStatus.requested => target is ProviderStatus.accepted or ProviderStatus.rejected,
            ProviderStatus.accepted => target is ProviderStatus.completed or ProviderStatus.terminated,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Không thể chuyển từ '{current}' sang '{target}' (contract type: {engagement.ContractType}).");
    }

    // ───────── Phân quyền theo vai trò trong chính engagement ─────────

    /// <summary>Vai trò của tài khoản đang đăng nhập ĐỐI VỚI engagement đang thao tác.</summary>
    private enum EngagementActor { Owner, Provider, Admin }

    /// <summary>
    /// Xác định người gọi là owner của project hay provider của engagement (admin đi cửa riêng).
    /// Engagement phải được load kèm ProjectShopOwner.Owner và ServiceProviderProfile.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không liên quan tới engagement (HTTP 401).</exception>
    private async Task<EngagementActor> ResolveActorAsync(long accountId, ProjectWorking engagement)
    {
        if (engagement.ProjectShopOwner?.Owner?.AccountId == accountId) return EngagementActor.Owner;
        if (engagement.ServiceProviderProfile?.AccountId == accountId) return EngagementActor.Provider;

        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        if (account?.Role == AccountRole.admin) return EngagementActor.Admin;

        throw new UnauthorizedAccessException(
            "Tài khoản đang đăng nhập không phải owner hay provider của engagement này.");
    }

    /// <summary>Admin luôn được phép; còn lại phải nằm trong danh sách vai trò cho phép.</summary>
    private static void EnsureActor(EngagementActor actual, string action, params EngagementActor[] allowed)
    {
        if (actual == EngagementActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" hoặc ", allowed.Select(a => a == EngagementActor.Owner ? "owner" : "provider"));
        throw new UnauthorizedAccessException($"Chỉ {who} của engagement mới được {action}.");
    }
}
