using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectPost;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectPost;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ProjectPostService : IProjectPostService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectPost> _repository;

    public ProjectPostService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectPost>();
    }

    public async Task<PaginationResponse<ProjectPostResponse>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        long? projectId = null,
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
                p => (projectId == null || p.ProjectId == projectId)
                     && (kind == null || p.ServiceKind == kind)
                     && (st == null || p.Status == st)
                     && (st != PostStatus.open || p.SubmissionDeadline == null || p.SubmissionDeadline > now)
                     && (term == null || EF.Functions.ILike(p.Title, $"%{term}%")),
                include: q => q.Include(p => p.Project))
            .OrderByDescending(p => p.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectPostResponse>(
            paged.Items.Select(ProjectPostResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectPostResponse> GetByIdAsync(long id)
    {
        var post = await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id,
            include: q => q.Include(p => p.Project))
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {id}.");

        return ProjectPostResponse.From(post);
    }

    public async Task<ProjectPostResponse> CreateAsync(CreateProjectPostRequest request)
    {
        var project = await _unitOfWork.GetRepository<Project>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.ProjectId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectId}.");

        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled)
            throw new InvalidOperationException(
                $"Project đang ở trạng thái '{project.Status}', không thể đăng bài tuyển.");

        if (!Enum.TryParse<ServiceKind>(request.ServiceKind, ignoreCase: true, out var kind))
            throw new ArgumentException(
                $"ServiceKind '{request.ServiceKind}' không hợp lệ. Cho phép: design, construction, both.");

        if (request.SubmissionDeadline.HasValue && request.SubmissionDeadline.Value <= DateTime.UtcNow)
            throw new ArgumentException("SubmissionDeadline phải nằm trong tương lai.");

        var post = new ProjectPost
        {
            ProjectId = project.Id,
            ServiceKind = kind,
            Title = request.Title,
            Description = request.Description,
            Status = PostStatus.open,
            SubmissionDeadline = request.SubmissionDeadline,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(post);
        await _unitOfWork.CommitAsync();

        post.Project = project;
        return ProjectPostResponse.From(post);
    }

    public async Task<ProjectPostResponse> UpdateAsync(long id, UpdateProjectPostRequest request)
    {
        var post = await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id,
            include: q => q.Include(p => p.Project))
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

        if (request.SubmissionDeadline.HasValue)
        {
            if (request.SubmissionDeadline.Value <= DateTime.UtcNow)
                throw new ArgumentException("SubmissionDeadline phải nằm trong tương lai.");
            post.SubmissionDeadline = request.SubmissionDeadline;
        }

        post.UpdatedAt = DateTime.UtcNow;
        _repository.Update(post);
        await _unitOfWork.CommitAsync();

        return ProjectPostResponse.From(post);
    }

    public async Task DeleteAsync(long id)
    {
        var post = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {id}.");

        _repository.Delete(post);
        await _unitOfWork.CommitAsync();
    }

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
