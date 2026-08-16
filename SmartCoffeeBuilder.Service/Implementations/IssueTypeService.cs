using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.DTOs.Requests.IssueType;
using SmartCoffeeBuilder.Service.DTOs.Responses.IssueType;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class IssueTypeService : IIssueTypeService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<IssueType> _repository;

    public IssueTypeService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<IssueType>();
    }

    public async Task<ICollection<IssueTypeResponse>> GetAllAsync()
    {
        var items = await _repository.GetListAsync(orderBy: q => q.OrderBy(t => t.Code));
        return items.Select(IssueTypeResponse.From).ToList();
    }

    public async Task<IssueTypeResponse> GetByIdAsync(Guid id)
    {
        var issueType = await _repository.SingleOrDefaultAsync(predicate: t => t.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy issue type với id {id}.");

        return IssueTypeResponse.From(issueType);
    }

    public async Task<IssueTypeResponse> CreateAsync(CreateIssueTypeRequest request)
    {
        var code = request.Code.Trim();
        var duplicated = await _repository.CountAsync(t => t.Code == code) > 0;
        if (duplicated)
            throw new InvalidOperationException($"Issue type với code '{code}' đã tồn tại.");

        var issueType = new IssueType
        {
            Code = code,
            Name = request.Name
        };

        await _repository.InsertAsync(issueType);
        await _unitOfWork.CommitAsync();

        return IssueTypeResponse.From(issueType);
    }

    public async Task<IssueTypeResponse> UpdateAsync(Guid id, UpdateIssueTypeRequest request)
    {
        var issueType = await _repository.SingleOrDefaultAsync(predicate: t => t.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy issue type với id {id}.");

        issueType.Name = request.Name;

        _repository.Update(issueType);
        await _unitOfWork.CommitAsync();

        return IssueTypeResponse.From(issueType);
    }

    public async Task DeleteAsync(Guid id)
    {
        var issueType = await _repository.SingleOrDefaultAsync(predicate: t => t.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy issue type với id {id}.");

        var inUse = await _unitOfWork.GetRepository<Issue>().CountAsync(i => i.IssueTypeId == id) > 0;
        if (inUse)
            throw new InvalidOperationException("Issue type đang được dùng bởi issue — không thể xoá.");

        _repository.Delete(issueType);
        await _unitOfWork.CommitAsync();
    }
}
