using SmartCoffeeBuilder.Service.DTOs.Requests.IssueType;
using SmartCoffeeBuilder.Service.DTOs.Responses.IssueType;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>Lookup danh mục loại issue — admin quản lý.</summary>
public interface IIssueTypeService
{
    Task<ICollection<IssueTypeResponse>> GetAllAsync();
    Task<IssueTypeResponse> GetByIdAsync(long id);
    Task<IssueTypeResponse> CreateAsync(CreateIssueTypeRequest request);
    Task<IssueTypeResponse> UpdateAsync(long id, UpdateIssueTypeRequest request);
    Task DeleteAsync(long id);
}
