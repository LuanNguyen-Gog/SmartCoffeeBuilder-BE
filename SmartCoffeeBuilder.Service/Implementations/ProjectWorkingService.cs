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

    // Trạng thái coi là "đang hoạt động" — chặn thuê trùng, cho phép terminate.
    private static readonly ProviderStatus[] ActiveStatuses =
    [
        ProviderStatus.requested, ProviderStatus.accepted
    ];

    public ProjectWorkingService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectWorking>();
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

        engagement.ProjectShopOwner = project;
        engagement.ServiceProviderProfile = provider;
        return ProjectWorkingResponse.From(engagement);
    }

    public async Task<ProjectWorkingResponse> UpdateStatusAsync(long id, UpdateProjectWorkingStatusRequest request)
    {
        if (!Enum.TryParse<ProviderStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.");

        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ProjectShopOwner)
                           .Include(e => e.ServiceProviderProfile)
                           .Include(e => e.Contracts))
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        ValidateTransition(engagement, target);

        // Nghiệm thu: engagement phải đã chạy thật (có contract confirmed) mới completed được.
        if (target == ProviderStatus.completed)
        {
            var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
                .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
            if (!hasConfirmedContract)
                throw new InvalidOperationException(
                    "Engagement chưa có contract 'confirmed' — chưa bắt đầu thực hiện nên không thể nghiệm thu.");
        }

        engagement.Status = target;
        engagement.UpdatedAt = DateTime.UtcNow;
        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        return ProjectWorkingResponse.From(engagement);
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
}
