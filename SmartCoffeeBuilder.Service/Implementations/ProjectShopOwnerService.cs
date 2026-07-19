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

public class ProjectShopOwnerService : IProjectShopOwnerService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectShopOwner> _repository;

    public ProjectShopOwnerService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectShopOwner>();
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

    public async Task<ProjectShopOwnerResponse> UpdateAsync(long id, UpdateProjectShopOwnerRequest request)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project == null || project.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy project với id {id}.");

        if (request.Name != null) project.Name = request.Name;
        if (request.Address != null) project.Address = request.Address;
        if (request.AreaM2.HasValue) project.AreaM2 = request.AreaM2.Value;
        if (request.Budget.HasValue) project.Budget = request.Budget.Value;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<ProjectStatus>(request.Status, ignoreCase: true, out var status))
                throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: briefed, in_progress, completed, cancelled.");
            project.Status = status;
        }

        project.UpdatedAt = DateTime.UtcNow;
        _repository.Update(project);
        await _unitOfWork.CommitAsync();

        return ProjectShopOwnerResponse.From(project);
    }

    public async Task DeleteAsync(long id)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project == null || project.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy project với id {id}.");

        project.DeletedAt = DateTime.UtcNow;
        _repository.Update(project);
        await _unitOfWork.CommitAsync();
    }
}
