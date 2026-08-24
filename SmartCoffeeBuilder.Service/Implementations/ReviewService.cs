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
    private readonly IFileStorageService _fileStorage;

    public ReviewService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Review>();
        _fileStorage = fileStorage;
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
                .ToDictionary(g => g.Key.ToString(), g => Math.Round((decimal)g.Average(s => s.Score), 2));
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

        var scores = ParseScores(request.Scores);

        var review = new Review
        {
            ProjectWorkingId = engagement.Id,
            OverallRating = request.OverallRating,
            Comment = request.Comment,
            ReviewScores = scores
                .Select(s => new ReviewScore { Dimension = s.Dimension, Score = s.Score })
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(review);
        await _unitOfWork.CommitAsync();

        await SyncProviderRatingAsync(engagement.ServiceProviderProfileId);

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
            var scores = ParseScores(request.Scores);

            // Thay thế toàn bộ điểm cũ bằng danh sách mới.
            _unitOfWork.GetRepository<ReviewScore>().DeleteRange(review.ReviewScores);
            review.ReviewScores = scores
                .Select(s => new ReviewScore { ReviewId = review.Id, Dimension = s.Dimension, Score = s.Score })
                .ToList();
        }

        review.UpdatedAt = DateTime.UtcNow;

        _repository.Update(review);
        await _unitOfWork.CommitAsync();

        await SyncProviderRatingAsync(review.ProjectWorking.ServiceProviderProfileId);

        return ReviewResponse.From(review);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var review = await _repository.SingleOrDefaultAsync(
            predicate: r => r.Id == id,
            include: q => q.Include(r => r.ProjectWorking))
            ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

        // Admin đi xuyên EnsureActor — gỡ đánh giá vi phạm là việc quản trị hợp lệ.
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "xoá đánh giá này", EngagementActor.Owner);

        var providerId = review.ProjectWorking.ServiceProviderProfileId;

        _repository.Delete(review); // review_score + review_image con cascade theo FK.
        await _unitOfWork.CommitAsync();

        await SyncProviderRatingAsync(providerId);
    }

    /// <summary>
    /// Đổi chuỗi client gửi lên thành <see cref="ReviewDimension"/> và chặn tiêu chí lặp.
    /// Tiêu chí là danh sách CỐ ĐỊNH: text tự do làm phần trung bình theo tiêu chí vỡ thành nhiều
    /// dòng gần giống nhau ("Tiến độ" / "Tien do") và không so sánh được giữa các provider.
    /// </summary>
    private static List<(ReviewDimension Dimension, int Score)> ParseScores(List<ReviewScoreRequest> scores)
    {
        var parsed = new List<(ReviewDimension, int)>();
        var seen = new HashSet<ReviewDimension>();

        foreach (var s in scores)
        {
            if (!Enum.TryParse<ReviewDimension>(s.Dimension?.Trim(), ignoreCase: true, out var dimension))
                throw new ArgumentException(
                    $"Dimension '{s.Dimension}' không hợp lệ. Cho phép: " +
                    $"{string.Join(", ", Enum.GetNames<ReviewDimension>())}.");

            if (!seen.Add(dimension))
                throw new ArgumentException($"Dimension '{dimension}' bị lặp — mỗi tiêu chí chỉ chấm 1 điểm.");

            parsed.Add((dimension, s.Score));
        }

        return parsed;
    }

    // ───────────────────────── Phản hồi & ảnh (review 1.1) ─────────────────────────

    /// <summary>
    /// Provider trả lời công khai một đánh giá. Mỗi review đúng MỘT phản hồi — gọi lại là ghi đè,
    /// không sinh thread: đây là quyền đáp lời, tranh luận qua lại đã có <c>conversations</c>.
    /// </summary>
    public async Task<ReviewResponse> ReplyAsync(Guid accountId, Guid id, ReplyReviewRequest request)
    {
        var review = await LoadGraphAsync(id);

        // CHỈ provider của engagement — owner tự "phản hồi" đánh giá của chính mình thì cột này vô nghĩa.
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "phản hồi đánh giá này", EngagementActor.Provider);

        if (string.IsNullOrWhiteSpace(request.Reply))
            throw new ArgumentException("Phản hồi không được để trống — dùng DELETE để gỡ phản hồi.");

        review.ProviderReply = request.Reply.Trim();
        review.RepliedBy = accountId;
        review.RepliedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        _repository.Update(review);
        await _unitOfWork.CommitAsync();

        return ReviewResponse.From(review);
    }

    /// <summary>Provider gỡ phản hồi của mình.</summary>
    public async Task<ReviewResponse> RemoveReplyAsync(Guid accountId, Guid id)
    {
        var review = await LoadGraphAsync(id);

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "gỡ phản hồi của đánh giá này", EngagementActor.Provider);

        review.ProviderReply = null;
        review.RepliedBy = null;
        review.RepliedAt = null;
        review.UpdatedAt = DateTime.UtcNow;

        _repository.Update(review);
        await _unitOfWork.CommitAsync();

        return ReviewResponse.From(review);
    }

    /// <summary>Chủ quán đính ảnh thành phẩm vào đánh giá của mình.</summary>
    public async Task<ReviewImageResponse> AddImageAsync(
        Guid accountId, Guid id, ReviewImageRequest request)
    {
        var review = await _repository.SingleOrDefaultAsync(predicate: r => r.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, review.ProjectWorkingId),
            "đính ảnh vào đánh giá này", EngagementActor.Owner);

        var objectName = await _fileStorage.NormalizeForStorageAsync(request.ImageUrl, "imageUrl")
            ?? throw new ArgumentException("Ảnh đánh giá phải có imageUrl.");

        var repo = _unitOfWork.GetRepository<ReviewImage>();
        var existing = await repo.GetListAsync(
            selector: i => i.SortOrder, predicate: i => i.ReviewId == review.Id);

        var image = new ReviewImage
        {
            ReviewId = review.Id,
            ImageUrl = objectName,
            Caption = request.Caption,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = DateTime.UtcNow
        };

        await repo.InsertAsync(image);
        await _unitOfWork.CommitAsync();

        return ReviewImageResponse.From(image);
    }

    public async Task RemoveImageAsync(Guid accountId, Guid imageId)
    {
        var repo = _unitOfWork.GetRepository<ReviewImage>();
        var image = await repo.SingleOrDefaultAsync(
            predicate: i => i.Id == imageId,
            include: q => q.Include(i => i.Review))
            ?? throw new KeyNotFoundException($"Không tìm thấy ảnh đánh giá với id {imageId}.");

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(
                _unitOfWork, accountId, image.Review.ProjectWorkingId),
            "xoá ảnh của đánh giá này", EngagementActor.Owner);

        var objectName = image.ImageUrl;

        repo.Delete(image);
        await _unitOfWork.CommitAsync();

        try { await _fileStorage.TryDeleteAsync(objectName); }
        catch { /* rác trên bucket không đáng để làm hỏng một request đã thành công */ }
    }

    private async Task<Review> LoadGraphAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: r => r.Id == id,
            include: q => q.Include(r => r.ReviewScores).Include(r => r.Images)
                           .Include(r => r.ProjectWorking))
        ?? throw new KeyNotFoundException($"Không tìm thấy review với id {id}.");

    /// <summary>
    /// Tính lại <c>service_providers.avg_rating</c> và <c>review_count</c> từ bảng <c>reviews</c>.
    ///
    /// Trước review 1.1 hai cột này KHÔNG BAO GIỜ được cập nhật (chỉ set 0 lúc tạo hồ sơ), trong khi
    /// <c>ServiceProviderProfileService.GetAllAsync</c> lại <c>OrderByDescending(p => p.AvgRating)</c>
    /// — nghĩa là danh sách provider đang sắp theo một cột luôn bằng 0.
    ///
    /// Tính lại TOÀN BỘ thay vì cộng dồn: số review mỗi provider nhỏ, mà cộng dồn thì mọi lần sửa
    /// hoặc xoá review đều là một cơ hội để con số trôi lệch vĩnh viễn.
    /// </summary>
    private async Task SyncProviderRatingAsync(Guid serviceProviderProfileId)
    {
        var ratings = await _repository.GetListAsync(
            selector: r => r.OverallRating,
            predicate: r => r.ProjectWorking.ServiceProviderProfileId == serviceProviderProfileId);

        var provider = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .SingleOrDefaultAsync(predicate: p => p.Id == serviceProviderProfileId);
        if (provider is null) return;

        provider.ReviewCount = ratings.Count;
        provider.AvgRating = ratings.Count == 0 ? 0m : Math.Round(ratings.Average(), 2);
        provider.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.GetRepository<ServiceProviderProfile>().Update(provider);
        await _unitOfWork.CommitAsync();
    }
}
