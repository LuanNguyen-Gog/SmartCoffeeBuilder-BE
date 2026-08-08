using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Post;
using SmartCoffeeBuilder.Service.DTOs.Responses.Post;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class PostService : IPostService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Post> _repository;

    public PostService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Post>();
    }

    public async Task<PaginationResponse<PostResponse>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        long? projectShopOwnerId = null,
        string? serviceKind = null,
        string? status = null,
        string? search = null)
    {
        ServiceKind? kind = ParseServiceKind(serviceKind);
        PostStatus? st = ParseStatus(status);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // Khi provider tìm bài đang mở, loại bài đã quá hạn nộp hồ sơ.
        var now = DateTime.UtcNow;
        var query = _repository
            .GetQueryable(
                p => p.ProjectShopOwner.DeletedAt == null // ẩn bài của dự án đã xoá mềm
                     && (projectShopOwnerId == null || p.ProjectShopOwnerId == projectShopOwnerId)
                     && (kind == null || p.ServiceKind == kind)
                     && (st == null || p.Status == st)
                     && (st != PostStatus.open || p.SubmissionDeadline == null || p.SubmissionDeadline > now)
                     && (term == null || EF.Functions.ILike(p.Title, $"%{term}%")),
                include: q => q.Include(p => p.ProjectShopOwner))
            // Bài trả phí boost còn hạn được ghim lên đầu, phần còn lại theo mới nhất.
            .OrderByDescending(p => p.BoostedUntil != null && p.BoostedUntil > now)
            .ThenByDescending(p => p.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<PostResponse>(
            paged.Items.Select(PostResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<PostResponse> GetByIdAsync(long id)
    {
        var post = await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id && p.ProjectShopOwner.DeletedAt == null,
            include: q => q.Include(p => p.ProjectShopOwner))
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {id}.");

        return PostResponse.From(post);
    }

    public async Task<PostResponse> CreateAsync(CreatePostRequest request)
    {
        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.ProjectShopOwnerId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectShopOwnerId}.");

        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled)
            throw new InvalidOperationException(
                $"ProjectShopOwner đang ở trạng thái '{project.Status}', không thể đăng bài tuyển.");

        if (!Enum.TryParse<ServiceKind>(request.ServiceKind, ignoreCase: true, out var kind))
            throw new ArgumentException(
                $"ServiceKind '{request.ServiceKind}' không hợp lệ. Cho phép: design, construction, both.");

        var deadline = ToDeadlineUtc(request.SubmissionDeadline);
        if (deadline.HasValue && deadline.Value <= DateTime.UtcNow)
            throw new ArgumentException("SubmissionDeadline phải là ngày hôm nay trở đi (theo giờ Việt Nam).");

        var post = new Post
        {
            ProjectShopOwnerId = project.Id,
            ServiceKind = kind,
            Title = request.Title,
            Description = request.Description,
            Status = PostStatus.open,
            SubmissionDeadline = deadline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(post);
        await _unitOfWork.CommitAsync();

        post.ProjectShopOwner = project;
        return PostResponse.From(post);
    }

    public async Task<PostResponse> UpdateAsync(long id, UpdatePostRequest request)
    {
        var post = await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id,
            include: q => q.Include(p => p.ProjectShopOwner))
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {id}.");

        if (request.Title != null) post.Title = request.Title;
        if (request.Description != null) post.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.ServiceKind))
        {
            if (!Enum.TryParse<ServiceKind>(request.ServiceKind, ignoreCase: true, out var kind))
                throw new ArgumentException(
                    $"ServiceKind '{request.ServiceKind}' không hợp lệ. Cho phép: design, construction, both.");
            post.ServiceKind = kind;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<PostStatus>(request.Status, ignoreCase: true, out var status))
                throw new ArgumentException(
                    $"Status '{request.Status}' không hợp lệ. Cho phép: open, closed, cancelled.");
            post.Status = status;
        }

        var deadline = ToDeadlineUtc(request.SubmissionDeadline);
        if (deadline.HasValue)
        {
            if (deadline.Value <= DateTime.UtcNow)
                throw new ArgumentException("SubmissionDeadline phải là ngày hôm nay trở đi (theo giờ Việt Nam).");
            post.SubmissionDeadline = deadline;
        }

        post.UpdatedAt = DateTime.UtcNow;
        _repository.Update(post);
        await _unitOfWork.CommitAsync();

        return PostResponse.From(post);
    }

    public async Task DeleteAsync(long id)
    {
        var post = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {id}.");

        _repository.Delete(post);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>Việt Nam không có DST nên offset cố định +07:00.</summary>
    private static readonly TimeSpan VietNamOffset = TimeSpan.FromHours(7);

    // Client chỉ gửi ngày (yyyy-MM-dd) → hạn chốt vào 23:59:59 cuối ngày đó theo giờ VN.
    // Trả về DateTime Kind=Utc vì cột submission_deadline là `timestamp with time zone`,
    // Npgsql chỉ ghi được Kind=Utc (Local/Unspecified ném InvalidCastException lúc SaveChanges).
    private static DateTime? ToDeadlineUtc(DateOnly? date) => date is null
        ? null
        : new DateTimeOffset(date.Value.ToDateTime(new TimeOnly(23, 59, 59)), VietNamOffset).UtcDateTime;

    private static ServiceKind? ParseServiceKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Enum.TryParse<ServiceKind>(value, ignoreCase: true, out var kind))
            throw new ArgumentException(
                $"ServiceKind '{value}' không hợp lệ. Cho phép: design, construction, both.");
        return kind;
    }

    private static PostStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Enum.TryParse<PostStatus>(value, ignoreCase: true, out var status))
            throw new ArgumentException(
                $"Status '{value}' không hợp lệ. Cho phép: open, closed, cancelled.");
        return status;
    }
}
