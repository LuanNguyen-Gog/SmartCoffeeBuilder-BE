using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ProjectProviderService : IProjectProviderService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectProvider> _repository;

    // Trạng thái coi là "đang hoạt động" — chặn thuê trùng, cho phép terminate.
    private static readonly ProviderStatus[] ActiveStatuses =
    [
        ProviderStatus.requested, ProviderStatus.accepted,
        ProviderStatus.designing, ProviderStatus.designed,
        ProviderStatus.constructing, ProviderStatus.constructed
    ];

    public ProjectProviderService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectProvider>();
    }

    public async Task<PaginationResponse<ProjectProviderResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectId = null, long? providerId = null, string? status = null)
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
                e => (projectId == null || e.ProjectId == projectId)
                     && (providerId == null || e.ProviderId == providerId)
                     && (st == null || e.Status == st),
                include: q => q.Include(e => e.Project).Include(e => e.Provider))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectProviderResponse>(
            paged.Items.Select(ProjectProviderResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectProviderResponse> GetByIdAsync(long id)
    {
        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.Project).Include(e => e.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        return ProjectProviderResponse.From(engagement);
    }

    public async Task<ProjectProviderResponse> CreateDirectRequestAsync(CreateProjectProviderRequest request)
    {
        var project = await _unitOfWork.GetRepository<Project>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.ProjectId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectId}.");

        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled)
            throw new InvalidOperationException($"Project đang ở trạng thái '{project.Status}', không thể thuê provider.");

        var provider = await _unitOfWork.GetRepository<ServiceProvider>()
            .SingleOrDefaultAsync(predicate: s => s.Id == request.ProviderId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {request.ProviderId}.");

        if (!Enum.TryParse<ServiceKind>(request.ContractType, ignoreCase: true, out var contractType))
            throw new ArgumentException($"ContractType '{request.ContractType}' không hợp lệ. Cho phép: design, construction, both.");

        var capabilityMatches = provider.Capability == Capability.both
            || (contractType == ServiceKind.design && provider.Capability == Capability.designer)
            || (contractType == ServiceKind.construction && provider.Capability == Capability.constructor);
        if (!capabilityMatches)
            throw new InvalidOperationException(
                $"Provider capability '{provider.Capability}' không phù hợp với contract type '{contractType}'.");

        var duplicated = await _repository.CountAsync(
            e => e.ProjectId == project.Id && e.ProviderId == provider.Id && ActiveStatuses.Contains(e.Status)) > 0;
        if (duplicated)
            throw new InvalidOperationException("Provider này đã có engagement đang hoạt động với project.");

        var engagement = new ProjectProvider
        {
            ProjectId = project.Id,
            ProviderId = provider.Id,
            ApplicationId = null,
            ContractType = contractType,
            Status = ProviderStatus.requested,
            RequestMessage = request.RequestMessage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(engagement);
        await _unitOfWork.CommitAsync();

        engagement.Project = project;
        engagement.Provider = provider;
        return ProjectProviderResponse.From(engagement);
    }

    public async Task<ProjectProviderResponse> UpdateStatusAsync(long id, UpdateProjectProviderStatusRequest request)
    {
        if (!Enum.TryParse<ProviderStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ.");

        var engagement = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.Project).Include(e => e.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {id}.");

        ValidateTransition(engagement, target);

        // TODO (chưa quyết định contract/quotation): khi chốt phương án ký kết,
        // thêm lại gate "phải có hợp đồng/báo giá confirmed" trước khi cho bắt đầu việc.
        var isStartingWork = engagement.Status == ProviderStatus.accepted
            && target is ProviderStatus.designing or ProviderStatus.constructing;
        if (isStartingWork)
        {
            engagement.StartedAt = DateTime.UtcNow;

            // Bắt đầu việc đầu tiên của project → project chuyển in_progress.
            if (engagement.Project.Status == ProjectStatus.briefed)
            {
                engagement.Project.Status = ProjectStatus.in_progress;
                engagement.Project.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.GetRepository<Project>().Update(engagement.Project);
            }
        }

        engagement.Status = target;
        engagement.UpdatedAt = DateTime.UtcNow;
        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        return ProjectProviderResponse.From(engagement);
    }

    private static void ValidateTransition(ProjectProvider engagement, ProviderStatus target)
    {
        var current = engagement.Status;

        // Kết thúc sớm: mọi trạng thái đang hoạt động (trừ requested — dùng rejected) đều terminate được.
        if (target == ProviderStatus.terminated)
        {
            if (current is ProviderStatus.rejected or ProviderStatus.completed or ProviderStatus.terminated
                or ProviderStatus.requested)
                throw new InvalidOperationException($"Không thể terminate engagement đang ở trạng thái '{current}'.");
            return;
        }

        var allowed = current switch
        {
            ProviderStatus.requested => target is ProviderStatus.accepted or ProviderStatus.rejected,
            ProviderStatus.accepted => (target == ProviderStatus.designing
                                            && engagement.ContractType is ServiceKind.design or ServiceKind.both)
                                       || (target == ProviderStatus.constructing
                                            && engagement.ContractType == ServiceKind.construction),
            ProviderStatus.designing => target == ProviderStatus.designed,
            ProviderStatus.designed => (target == ProviderStatus.constructing
                                            && engagement.ContractType == ServiceKind.both)
                                       || target == ProviderStatus.completed,
            ProviderStatus.constructing => target == ProviderStatus.constructed,
            ProviderStatus.constructed => target == ProviderStatus.completed,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Không thể chuyển từ '{current}' sang '{target}' (contract type: {engagement.ContractType}).");
    }
}
