using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class DesignBriefService : IDesignBriefService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<DesignBrief> _repository;

    public DesignBriefService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<DesignBrief>();
    }

    public async Task<PaginationResponse<DesignBriefResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? projectId = null)
    {
        var query = _repository
            .GetQueryable(b => projectId == null || b.ProjectId == projectId)
            .OrderByDescending(b => b.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DesignBriefResponse>(
            paged.Items.Select(DesignBriefResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DesignBriefResponse> GetByIdAsync(long id)
    {
        var brief = await _repository.SingleOrDefaultAsync(predicate: b => b.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {id}.");

        return DesignBriefResponse.From(brief);
    }

    public async Task<DesignBriefResponse> CreateAsync(CreateDesignBriefRequest request)
    {
        _ = await _unitOfWork.GetRepository<Project>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.ProjectId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectId}.");

        // DB có unique index trên project_id — check trước để trả 409 thay vì 500.
        if (await _repository.CountAsync(b => b.ProjectId == request.ProjectId) > 0)
            throw new InvalidOperationException($"Project {request.ProjectId} đã có design brief.");

        var brief = new DesignBrief
        {
            ProjectId = request.ProjectId,
            TargetCustomer = request.TargetCustomer,
            Style = request.Style,
            Mood = request.Mood,
            SeatCount = request.SeatCount,
            Timeline = request.Timeline,
            BrandNote = request.BrandNote,
            BusinessModel = request.BusinessModel,
            BusinessGoals = request.BusinessGoals,
            OperationNote = request.OperationNote,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(brief);
        await _unitOfWork.CommitAsync();

        return DesignBriefResponse.From(brief);
    }

    public async Task<DesignBriefResponse> UpdateAsync(long id, UpdateDesignBriefRequest request)
    {
        var brief = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {id}.");

        if (request.TargetCustomer != null) brief.TargetCustomer = request.TargetCustomer;
        if (request.Style != null) brief.Style = request.Style;
        if (request.Mood != null) brief.Mood = request.Mood;
        if (request.SeatCount.HasValue) brief.SeatCount = request.SeatCount;
        if (request.Timeline != null) brief.Timeline = request.Timeline;
        if (request.BrandNote != null) brief.BrandNote = request.BrandNote;
        if (request.BusinessModel != null) brief.BusinessModel = request.BusinessModel;
        if (request.BusinessGoals != null) brief.BusinessGoals = request.BusinessGoals;
        if (request.OperationNote != null) brief.OperationNote = request.OperationNote;

        brief.UpdatedAt = DateTime.UtcNow;
        _repository.Update(brief);
        await _unitOfWork.CommitAsync();

        return DesignBriefResponse.From(brief);
    }

    public async Task DeleteAsync(long id)
    {
        var brief = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {id}.");

        _repository.Delete(brief);
        await _unitOfWork.CommitAsync();
    }
}
