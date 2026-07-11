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
        ProviderStatus.requested, ProviderStatus.accepted
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

        // Nghiệm thu: engagement phải đã chạy thật (có contract confirmed) mới completed được.
        if (target == ProviderStatus.completed)
        {
            var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
                .CountAsync(c => c.ProjectProviderId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
            if (!hasConfirmedContract)
                throw new InvalidOperationException(
                    "Engagement chưa có contract 'confirmed' — chưa bắt đầu thực hiện nên không thể nghiệm thu.");
        }

        engagement.Status = target;
        engagement.UpdatedAt = DateTime.UtcNow;
        _repository.Update(engagement);
        await _unitOfWork.CommitAsync();

        return ProjectProviderResponse.From(engagement);
    }

    // v5 — provider_status là trạng thái QUAN HỆ, không phải tiến độ:
    // requested → accepted | rejected; accepted → completed | terminated.
    // Tiến độ (designing/constructing…) là derived từ contract_type + design/construction_item con.
    private static void ValidateTransition(ProjectProvider engagement, ProviderStatus target)
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
