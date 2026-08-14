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
using SmartCoffeeBuilder.Service.Utils;

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
                e => e.ProjectShopOwner.DeletedAt == null            // ẩn engagement của dự án đã xoá mềm
                     && e.ServiceProviderProfile.DeletedAt == null    // hoặc của provider đã xoá mềm
                     && (projectShopOwnerId == null || e.ProjectShopOwnerId == projectShopOwnerId)
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

    public async Task<PaginationResponse<ProjectWorkingResponse>> FilterAsync(
        int pageNumber = 1, int pageSize = 10, string? statuses = null,
        long? projectShopOwnerId = null, long? serviceProviderProfileId = null, string? contractType = null)
    {
        // Danh sách rỗng = không lọc theo trạng thái (Contains dịch được sang SQL IN).
        var statusFilter = ParseStatuses(statuses);

        ServiceKind? kind = null;
        if (!string.IsNullOrWhiteSpace(contractType))
        {
            if (!Enum.TryParse<ServiceKind>(contractType, ignoreCase: true, out var parsedKind))
                throw new ArgumentException($"ContractType '{contractType}' không hợp lệ. Cho phép: design, construction, both.");
            kind = parsedKind;
        }

        var query = _repository
            .GetQueryable(
                e => e.ProjectShopOwner.DeletedAt == null
                     && e.ServiceProviderProfile.DeletedAt == null
                     && (projectShopOwnerId == null || e.ProjectShopOwnerId == projectShopOwnerId)
                     && (serviceProviderProfileId == null || e.ServiceProviderProfileId == serviceProviderProfileId)
                     && (kind == null || e.ContractType == kind)
                     && (statusFilter.Count == 0 || statusFilter.Contains(e.Status)),
                include: q => q.Include(e => e.ProjectShopOwner)
                               .Include(e => e.ServiceProviderProfile)
                               .Include(e => e.Contracts))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectWorkingResponse>(
            paged.Items.Select(ProjectWorkingResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    /// <summary>"requested,accepted" → danh sách enum. Rỗng/null → danh sách rỗng = lấy tất cả.</summary>
    private static List<ProviderStatus> ParseStatuses(string? statuses)
    {
        if (string.IsNullOrWhiteSpace(statuses)) return [];

        var result = new List<ProviderStatus>();
        foreach (var raw in statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<ProviderStatus>(raw, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{raw}' không hợp lệ. Cho phép: requested, accepted, rejected, completed, terminated.");
            if (!result.Contains(parsed)) result.Add(parsed);
        }

        return result;
    }

    public async Task<ProjectWorkingResponse> GetByIdAsync(long id)
    {
        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id
                            && e.ProjectShopOwner.DeletedAt == null
                            && e.ServiceProviderProfile.DeletedAt == null,
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

        // Mỗi dự án chỉ có MỘT chỗ design và MỘT chỗ construction — xem ProjectSlotRules.
        // Capability 'both' không phá luật này: muốn vào dự án đã có designer thì phải mời với
        // phạm vi 'construction', không mời được 'design' lẫn 'both'.
        var activeKinds = await _repository.GetListAsync(
            selector: e => e.ContractType,
            predicate: e => e.ProjectShopOwnerId == project.Id && ActiveStatuses.Contains(e.Status));
        ProjectSlotRules.EnsureSlotFree(
            activeKinds, contractType, $"mời provider với phạm vi '{contractType}'");

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

        // Lời mời này giữ chỗ ngay từ 'requested' — bài đăng nào của dự án đụng vào chỗ đó coi như
        // tuyển không nổi nữa, đóng luôn và từ chối hồ sơ đang chờ thay vì để provider chờ vô ích.
        var rejectedApplicationIds = await ProjectSlotClosure.CloseCoveredPostsAsync(
            _unitOfWork, project.Id, contractType);

        await _unitOfWork.CommitAsync();

        // Sau khi lưu — báo cho PROVIDER biết họ vừa được mời hợp tác trực tiếp.
        await _notificationService.NotifyEngagementInvitedAsync(engagement.Id);

        // ...và cho các provider có hồ sơ bị đóng theo.
        foreach (var applicationId in rejectedApplicationIds)
            await _notificationService.NotifyApplicationDecisionAsync(applicationId, accepted: false);

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

    public async Task<ProjectWorkingResponse> UpdateStatusAsync(
        long accountId, long id, UpdateProjectWorkingStatusRequest request)
    {
        if (!Enum.TryParse<ProviderStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.");

        // Huỷ ngang KHÔNG đi thẳng qua state machine nữa — phải qua luồng đồng thuận hai bên.
        if (target == ProviderStatus.terminated)
            return await TerminateAsync(accountId, id);

        // Còn lại: endpoint tổng chỉ là cửa vào — mọi kiểm tra nằm trong TransitionAsync để một luật duy nhất.
        return await TransitionAsync(accountId, id, target);
    }

    // ───────── Huỷ ngang cần ĐỒNG THUẬN HAI BÊN ─────────

    public async Task<ProjectWorkingResponse> RequestTerminationAsync(
        long accountId, long id, RequestEngagementTerminationRequest request)
    {
        var engagement = await LoadForActionAsync(id);
        var actor = await ResolveActorAsync(accountId, engagement);
        EnsureActor(actor, "đề nghị huỷ ngang engagement", EngagementActor.Owner, EngagementActor.Provider);
        EnsureTerminable(engagement);

        // Admin không phải một "bên" của engagement — can thiệp hành chính thì huỷ thẳng.
        if (ToParty(actor) is not EngagementParty party)
            return await ForceTerminateAsync(engagement);

        if (engagement.TerminationRequestedAt != null)
            throw new InvalidOperationException(
                engagement.TerminationRequestedBy == party
                    ? "Bạn đã gửi đề nghị huỷ ngang cho hợp tác này — đang chờ bên kia phản hồi."
                    : "Bên kia đã đề nghị huỷ ngang — hãy đồng ý hoặc từ chối đề nghị đó thay vì gửi đề nghị mới.");

        engagement.TerminationRequestedAt = DateTime.UtcNow;
        engagement.TerminationRequestedBy = party;
        engagement.TerminationRequestNote = request.Reason;
        engagement.UpdatedAt = DateTime.UtcNow;

        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — bên còn lại nhận noti + email để vào đồng ý hoặc từ chối.
        await _notificationService.NotifyEngagementTerminationRequestedAsync(
            engagement.Id, requestedByOwner: party == EngagementParty.owner);

        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ProjectWorkingResponse> RespondTerminationAsync(
        long accountId, long id, RespondEngagementTerminationRequest request)
    {
        var engagement = await LoadForActionAsync(id);
        var actor = await ResolveActorAsync(accountId, engagement);
        EnsureActor(actor, "phản hồi đề nghị huỷ ngang", EngagementActor.Owner, EngagementActor.Provider);
        EnsureTerminable(engagement);

        var requester = EnsurePendingTerminationRequest(engagement);

        // Người đề nghị không tự duyệt đề nghị của chính mình (admin phản hồi thay được).
        if (ToParty(actor) is EngagementParty party && party == requester)
            throw new InvalidOperationException(
                "Bạn là bên gửi đề nghị huỷ ngang — chỉ bên còn lại mới được đồng ý hoặc từ chối. " +
                "Muốn dừng lại thì rút đề nghị (DELETE /termination-request).");

        var now = DateTime.UtcNow;
        if (request.Approve)
        {
            // Đồng ý → chốt huỷ. Giữ nguyên termination_requested_* làm vết ai đã đề nghị.
            CloseAsTerminated(engagement, now);
        }
        else
        {
            // Từ chối → xoá đề nghị, hợp tác chạy tiếp như chưa có gì.
            ClearTerminationRequest(engagement);
        }

        engagement.UpdatedAt = now;
        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — bên ĐỀ NGHỊ nhận noti + email biết kết quả.
        await _notificationService.NotifyEngagementTerminationDecisionAsync(
            engagement.Id, requestedByOwner: requester == EngagementParty.owner, approved: request.Approve);

        if (request.Approve)
            await _notificationService.NotifyProjectReadyToCloseAsync(engagement.ProjectShopOwnerId);

        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ProjectWorkingResponse> CancelTerminationRequestAsync(long accountId, long id)
    {
        var engagement = await LoadForActionAsync(id);
        var actor = await ResolveActorAsync(accountId, engagement);
        EnsureActor(actor, "rút lại đề nghị huỷ ngang", EngagementActor.Owner, EngagementActor.Provider);
        EnsureTerminable(engagement);

        var requester = EnsurePendingTerminationRequest(engagement);

        if (ToParty(actor) is EngagementParty party && party != requester)
            throw new InvalidOperationException(
                "Chỉ bên đã gửi đề nghị mới rút lại được — bạn có thể từ chối đề nghị này thay vì rút.");

        ClearTerminationRequest(engagement);
        engagement.UpdatedAt = DateTime.UtcNow;

        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Sau khi lưu — bên còn lại biết là không cần phản hồi nữa.
        await _notificationService.NotifyEngagementTerminationCancelledAsync(
            engagement.Id, requestedByOwner: requester == EngagementParty.owner);

        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ProjectWorkingResponse> TerminateAsync(long accountId, long id)
    {
        var engagement = await LoadForActionAsync(id);
        var actor = await ResolveActorAsync(accountId, engagement);
        EnsureActor(actor, "huỷ ngang engagement", EngagementActor.Owner, EngagementActor.Provider);
        EnsureTerminable(engagement);

        // Admin can thiệp hành chính — huỷ thẳng, không cần đồng thuận.
        if (ToParty(actor) is not EngagementParty party)
            return await ForceTerminateAsync(engagement);

        // Bên kia đã đề nghị rồi → bấm "huỷ ngang" chính là đồng ý.
        if (engagement.TerminationRequestedAt != null && engagement.TerminationRequestedBy != party)
            return await RespondTerminationAsync(accountId, id, new RespondEngagementTerminationRequest { Approve = true });

        // Còn lại (chưa có đề nghị, hoặc chính mình đã đề nghị) → RequestTermination tự trả lỗi đúng ngữ cảnh.
        return await RequestTerminationAsync(accountId, id, new RequestEngagementTerminationRequest());
    }

    /// <summary>
    /// Admin huỷ thẳng, bỏ qua đồng thuận. Cả hai bên đều nhận noti + email vì không bên nào chủ động.
    /// </summary>
    private async Task<ProjectWorkingResponse> ForceTerminateAsync(ProjectWorking engagement)
    {
        var now = DateTime.UtcNow;
        CloseAsTerminated(engagement, now);
        ClearTerminationRequest(engagement);
        engagement.UpdatedAt = now;

        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        // Gọi hai lần = gửi cho cả provider (true) lẫn owner (false) — xem doc của method.
        await _notificationService.NotifyEngagementTerminatedAsync(engagement.Id, terminatedByOwner: true);
        await _notificationService.NotifyEngagementTerminatedAsync(engagement.Id, terminatedByOwner: false);
        await _notificationService.NotifyProjectReadyToCloseAsync(engagement.ProjectShopOwnerId);

        return ProjectWorkingResponse.From(engagement);
    }

    /// <summary>Huỷ ngang chỉ áp dụng cho hợp tác đang chạy.</summary>
    private static void EnsureTerminable(ProjectWorking engagement)
    {
        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ huỷ ngang được hợp tác 'accepted'.");
    }

    /// <summary>Phải có đề nghị đang treo mới phản hồi/rút được. Trả về bên đã gửi đề nghị.</summary>
    private static EngagementParty EnsurePendingTerminationRequest(ProjectWorking engagement)
    {
        if (engagement.TerminationRequestedAt == null
            || engagement.TerminationRequestedBy is not EngagementParty requester)
            throw new InvalidOperationException("Hợp tác này không có đề nghị huỷ ngang nào đang chờ xử lý.");

        return requester;
    }

    private static void ClearTerminationRequest(ProjectWorking engagement)
    {
        engagement.TerminationRequestedAt = null;
        engagement.TerminationRequestedBy = null;
        engagement.TerminationRequestNote = null;
    }

    /// <summary>
    /// Chốt engagement sang 'terminated'. KHÔNG commit — caller gộp chung một SaveChanges.
    /// Không đụng tới bài đăng/hồ sơ nguồn: huỷ ngang chỉ đóng đúng engagement này.
    /// </summary>
    private static void CloseAsTerminated(ProjectWorking engagement, DateTime now)
    {
        engagement.Status = ProviderStatus.terminated;
        engagement.TerminatedAt = now;
        // Yêu cầu nghiệm thu đang treo không còn ý nghĩa.
        engagement.CompletionRequestedAt = null;
        engagement.CompletionRequestNote = null;
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

            // Huỷ ngang KHÔNG đi qua đây nữa: cần đồng thuận hai bên (RequestTermination →
            // RespondTermination). UpdateStatusAsync đã chuyển hướng, nên tới được đây là gọi sai.
            case ProviderStatus.terminated:
                throw new InvalidOperationException(
                    "Huỷ ngang cần đồng thuận hai bên — dùng POST /{id}/termination-request rồi để bên còn lại " +
                    "phản hồi qua POST /{id}/termination-response.");

            default:
                throw new ArgumentException($"Status '{target}' không phải trạng thái đích hợp lệ.");
        }

        ValidateTransition(engagement, target);

        if (target == ProviderStatus.completed)
        {
            // Nghiệm thu: engagement phải đã chạy thật (có contract confirmed) mới completed được.
            await EnsureConfirmedContractAsync(engagement);

            // Sản phẩm bàn giao phải THỰC SỰ xong (design approved / mọi milestone completed) —
            // kiểm LẠI tại thời điểm nghiệm thu, không tin vào lần provider bấm "báo hoàn thành".
            // Trước đây có đề nghị hoàn thành là bỏ qua bước này, nên provider báo xong rồi thêm
            // milestone mới là owner nghiệm thu được một engagement còn hạng mục 'pending'.
            // Chỉ xét phần việc CỦA CHÍNH engagement này — phía kia xong hay chưa không liên quan.
            // Designer được nghiệm thu và nhận review ngay khi bản vẽ duyệt, không phải chờ công
            // trình xây xong. Ràng buộc "đủ cả hai phía" nằm ở bước đóng dự án (ProjectClosureRules).
            await EnsureDeliverablesReadyAsync(engagement, "chưa thể nghiệm thu");
        }

        engagement.Status = target;
        // Nghiệm thu xong thì đề nghị huỷ ngang đang treo (nếu có) không còn ý nghĩa.
        if (target == ProviderStatus.completed) ClearTerminationRequest(engagement);
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

        // Đóng một engagement có thể là mảnh ghép cuối của cả dự án — nhắc owner bấm đóng dự án,
        // nếu không dự án nằm mãi ở 'in_progress' dù mọi hợp tác đã xong. Điều kiện đủ do
        // NotificationService tự xét (trùng guard của ProjectShopOwnerService.CompleteAsync).
        // (Nhánh huỷ ngang gọi lời nhắc này trong RespondTerminationAsync/ForceTerminateAsync.)
        if (target == ProviderStatus.completed)
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

    /// <summary>
    /// Quy vai trò trong engagement về "bên" lưu trong DB. Admin trả null — admin đứng NGOÀI
    /// hai bên nên không đề nghị/đồng ý huỷ ngang thay ai được, chỉ can thiệp huỷ thẳng.
    /// </summary>
    private static EngagementParty? ToParty(EngagementActor actor) => actor switch
    {
        EngagementActor.Owner => EngagementParty.owner,
        EngagementActor.Provider => EngagementParty.provider,
        _ => null
    };

    /// <summary>Admin luôn được phép; còn lại phải nằm trong danh sách vai trò cho phép.</summary>
    private static void EnsureActor(EngagementActor actual, string action, params EngagementActor[] allowed)
    {
        if (actual == EngagementActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" hoặc ", allowed.Select(a => a == EngagementActor.Owner ? "owner" : "provider"));
        throw new UnauthorizedAccessException($"Chỉ {who} của engagement mới được {action}.");
    }
}
