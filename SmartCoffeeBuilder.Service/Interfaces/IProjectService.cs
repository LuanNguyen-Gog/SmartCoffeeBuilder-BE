using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Project;
using SmartCoffeeBuilder.Service.DTOs.Responses.Project;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectService
{
    Task<PaginationResponse<ProjectResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? ownerId = null);
    Task<ProjectResponse> GetByIdAsync(long id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request);
    Task<ProjectResponse> UpdateAsync(long id, UpdateProjectRequest request);
    Task DeleteAsync(long id);
}
