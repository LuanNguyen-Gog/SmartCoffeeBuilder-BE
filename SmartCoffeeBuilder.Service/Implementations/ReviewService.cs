using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.DTOs.Responses.Review;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Review> _repository;

    public ReviewService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Review>();
    }

    public async Task<PaginationResponse<ReviewResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? serviceProviderProfileId = null)
    {
        var query = _repository
            .GetQueryable(
                r => (projectWorkingId == null || r.ProjectWorkingId == projectWorkingId)
                     && (serviceProviderProfileId == null || r.ProjectWorking.ServiceProviderProfileId == serviceProviderProfileId),
                include: q => q.Include(r => r.ReviewScores).Include(r => r.ProjectWorking))
            .OrderByDescending(r => r.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ReviewResponse>(
            paged.Items.Select(ReviewResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ReviewResponse> GetByIdAsync(Guid id)
    {
        var review = await _repository.SingleOrDefaultAsync(
            predicate: r => r.Id == id,
            include: q => q.Include(r => r.ReviewScores).Include(r => r.ProjectWorking))
            ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

        return ReviewResponse.From(review);
    }

    public async Task<ProviderRatingSummaryResponse> GetProviderSummaryAsync(Guid serviceProviderProfileId)
    {
        _ = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .SingleOrDefaultAsync(predicate: s => s.Id == serviceProviderProfileId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {serviceProviderProfileId}.");

        var reviews = await _repository.GetListAsync(
            predicate: r => r.ProjectWorking.ServiceProviderProfileId == serviceProviderProfileId,
            include: q => q.Include(r => r.ReviewScores));

        var summary = new ProviderRatingSummaryResponse
        {
            ServiceProviderProfileId = serviceProviderProfileId,
            ReviewCount = reviews.Count
        };

        if (reviews.Count > 0)
        {
            summary.AverageRating = Math.Round(reviews.Average(r => r.OverallRating), 2);
            summary.DimensionAverages = reviews
                .SelectMany(r => r.ReviewScores)
                .GroupBy(s => s.Dimension)
                .ToDictionary(g => g.Key, g => Math.Round((decimal)g.Average(s => s.Score), 2));
        }

        return summary;
    }

    public async Task<ReviewResponse> CreateAsync(Guid accountId, CreateReviewRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectWorkingId}.");

        // Quyền TRƯỚC mọi check trạng thái — role gate 'owner' không phân biệt được owner NÀO,
        // thiếu chỗ này thì owner bất kỳ chấm điểm hộ được engagement của người khác, và điểm đó
        // chảy thẳng vào rating trung bình của provider.
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagement.Id),
            "đánh giá hợp tác này", EngagementActor.Owner);

        // v5: review chỉ mở khoá sau khi owner nghiệm thu (provider_status = completed).
        if (engagement.Status != ProviderStatus.completed)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ review được sau khi owner nghiệm thu ('completed').");

        var alreadyReviewed = await _repository.CountAsync(r => r.ProjectWorkingId == engagement.Id) > 0;
        if (alreadyReviewed)
            throw new InvalidOperationException("Engagement này đã có review — mỗi engagement chỉ review 1 lần, dùng PUT để sửa.");

        ValidateScores(request.Scores);

        var review = new Review
        {
            ProjectWorkingId = engagement.Id,
            OverallRating = request.OverallRating,
            Comment = request.Comment,
            ReviewScores = request.Scores
                .Select(s => new ReviewScore { Dimension = s.Dimension.Trim(), Score = s.Score })
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(review);
        await _unitOfWork.CommitAsync();

        review.ProjectWorking = engagement;
        return ReviewResponse.From(review);
    }

    public async Task<ReviewResponse> UpdateAsync(Guid accountId, Guid id, UpdateReviewRequest request)
    {
        var review = await _repository.SingleOrDefaultAsync(
            predicate: r => r.Id == id,
            include: q => q.Include(r => r.ReviewScores).Include(r => r.ProjectWorking))
            ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "sửa đánh giá này", EngagementActor.Owner);

        if (request.OverallRating.HasValue) review.OverallRating = request.OverallRating.Value;
        if (request.Comment != null) review.Comment = request.Comment;

        if (request.Scores != null)
        {
            ValidateScores(request.Scores);

            // Thay thế toàn bộ điểm cũ bằng danh sách mới.
            _unitOfWork.GetRepository<ReviewScore>().DeleteRange(review.ReviewScores);
            review.ReviewScores = request.Scores
                .Select(s => new ReviewScore { ReviewId = review.Id, Dimension = s.Dimension.Trim(), Score = s.Score })
                .ToList();
        }

        review.UpdatedAt = DateTime.UtcNow;

        _repository.Update(review);
        await _unitOfWork.CommitAsync();

        return ReviewResponse.From(review);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var review = await _repository.SingleOrDefaultAsync(predicate: r => r.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

        // Admin đi xuyên EnsureActor — gỡ đánh giá vi phạm là việc quản trị hợp lệ.
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "xoá đánh giá này", EngagementActor.Owner);

        _repository.Delete(review); // review_score con cascade theo FK.
        await _unitOfWork.CommitAsync();
    }

    private static void ValidateScores(List<ReviewScoreRequest> scores)
    {
        var duplicated = scores
            .GroupBy(s => s.Dimension.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicated != null)
            throw new ArgumentException($"Dimension '{duplicated.Key}' bị lặp — mỗi tiêu chí chỉ chấm 1 điểm.");
    }
}
