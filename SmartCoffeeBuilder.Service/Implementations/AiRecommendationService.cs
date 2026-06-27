using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class AiRecommendationService : IAiRecommendationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<AiRecommendation> _repository;

    public AiRecommendationService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<AiRecommendation>();
    }

    public async Task<PaginationResponse<AiRecommendationResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? briefId = null)
    {
        var query = _repository
            .GetQueryable(r => briefId == null || r.BriefId == briefId)
            .OrderByDescending(r => r.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<AiRecommendationResponse>(
            paged.Items.Select(AiRecommendationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<AiRecommendationResponse> GetByIdAsync(long id)
    {
        var recommendation = await _repository.SingleOrDefaultAsync(predicate: r => r.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request)
    {
        _ = await _unitOfWork.GetRepository<DesignBrief>()
            .SingleOrDefaultAsync(predicate: b => b.Id == request.BriefId)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {request.BriefId}.");

        var recommendation = new AiRecommendation
        {
            BriefId = request.BriefId,
            ConceptSummary = request.ConceptSummary,
            Payload = request.Payload,
            EstimatedDesignCost = request.EstimatedDesignCost,
            EstimatedConstructionCost = request.EstimatedConstructionCost,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(recommendation);
        await _unitOfWork.CommitAsync();

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task<AiRecommendationResponse> UpdateAsync(long id, UpdateAiRecommendationRequest request)
    {
        var recommendation = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        if (request.ConceptSummary != null) recommendation.ConceptSummary = request.ConceptSummary;
        if (request.Payload != null) recommendation.Payload = request.Payload;
        if (request.EstimatedDesignCost.HasValue) recommendation.EstimatedDesignCost = request.EstimatedDesignCost;
        if (request.EstimatedConstructionCost.HasValue) recommendation.EstimatedConstructionCost = request.EstimatedConstructionCost;

        _repository.Update(recommendation);
        await _unitOfWork.CommitAsync();

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task DeleteAsync(long id)
    {
        var recommendation = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        _repository.Delete(recommendation);
        await _unitOfWork.CommitAsync();
    }
}
