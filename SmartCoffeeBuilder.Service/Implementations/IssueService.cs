using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Issue;
using SmartCoffeeBuilder.Service.DTOs.Responses.Issue;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class IssueService : IIssueService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Issue> _repository;
    private readonly IFileStorageService _fileStorage;

    public IssueService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Issue>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<IssueResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? constructionItemId = null, string? status = null)
    {
        IssueStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<IssueStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: open, in_progress, resolved, closed.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(
                e => (projectWorkingId == null || e.ProjectWorkingId == projectWorkingId)
                     && (constructionItemId == null || e.ConstructionItemId == constructionItemId)
                     && (st == null || e.Status == st),
                include: q => q.Include(e => e.IssueType))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<IssueResponse>(
            paged.Items.Select(IssueResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<IssueResponse> GetByIdAsync(Guid id)
    {
        var issue = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.IssueType))
            ?? throw new KeyNotFoundException($"Không tìm thấy issue với id {id}.");

        return IssueResponse.From(issue);
    }

    public async Task<IssueResponse> CreateAsync(CreateIssueRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectWorkingId}.");

        var issueType = await _unitOfWork.GetRepository<IssueType>()
            .SingleOrDefaultAsync(predicate: t => t.Id == request.IssueTypeId)
            ?? throw new KeyNotFoundException($"Không tìm thấy issue type với id {request.IssueTypeId}.");

        if (request.ConstructionItemId != null)
        {
            var item = await _unitOfWork.GetRepository<ConstructionItem>()
                .SingleOrDefaultAsync(predicate: e => e.Id == request.ConstructionItemId)
                ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {request.ConstructionItemId}.");
            if (item.ProjectWorkingId != engagement.Id)
                throw new InvalidOperationException("Construction item phải thuộc cùng engagement với issue.");
        }

        if (request.CreatedBy != null)
        {
            _ = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == request.CreatedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.CreatedBy}.");
        }

        var issue = new Issue
        {
            ProjectWorkingId = engagement.Id,
            ConstructionItemId = request.ConstructionItemId,
            IssueTypeId = issueType.Id,
            Cause = request.Cause,
            Reason = request.Reason,
            Solution = request.Solution,
            // Ảnh phải upload qua api/files trước; giá trị gửi lên được rút về ObjectName.
            IssueImage = await _fileStorage.NormalizeForStorageAsync(request.IssueImage, "issueImage"),
            ConfirmImage = await _fileStorage.NormalizeForStorageAsync(request.ConfirmImage, "confirmImage"),
            EstimateAt = request.EstimateAt,
            Status = IssueStatus.open,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(issue);
        await _unitOfWork.CommitAsync();

        issue.IssueType = issueType;
        return IssueResponse.From(issue);
    }

    public async Task<IssueResponse> UpdateAsync(Guid id, UpdateIssueRequest request)
    {
        var issue = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.IssueType))
            ?? throw new KeyNotFoundException($"Không tìm thấy issue với id {id}.");

        if (issue.Status == IssueStatus.closed)
            throw new InvalidOperationException("Issue đã 'closed' — không chỉnh sửa được nữa.");

        if (request.IssueTypeId.HasValue && request.IssueTypeId.Value != issue.IssueTypeId)
        {
            var issueType = await _unitOfWork.GetRepository<IssueType>()
                .SingleOrDefaultAsync(predicate: t => t.Id == request.IssueTypeId.Value)
                ?? throw new KeyNotFoundException($"Không tìm thấy issue type với id {request.IssueTypeId.Value}.");
            issue.IssueTypeId = issueType.Id;
            issue.IssueType = issueType;
        }
        if (request.Cause != null) issue.Cause = request.Cause;
        if (request.Reason != null) issue.Reason = request.Reason;
        if (request.Solution != null) issue.Solution = request.Solution;
        // Ảnh cũ bị thay thì dọn luôn object trên bucket (sau khi DB commit) để khỏi rác.
        string? replacedIssueImage = null;
        if (request.IssueImage != null)
        {
            var newImage = await _fileStorage.NormalizeForStorageAsync(request.IssueImage, "issueImage");
            if (newImage != issue.IssueImage) replacedIssueImage = issue.IssueImage;
            issue.IssueImage = newImage;
        }

        string? replacedConfirmImage = null;
        if (request.ConfirmImage != null)
        {
            var newImage = await _fileStorage.NormalizeForStorageAsync(request.ConfirmImage, "confirmImage");
            if (newImage != issue.ConfirmImage) replacedConfirmImage = issue.ConfirmImage;
            issue.ConfirmImage = newImage;
        }

        if (request.EstimateAt.HasValue) issue.EstimateAt = request.EstimateAt.Value;
        issue.UpdatedAt = DateTime.UtcNow;

        _repository.Update(issue);
        await _unitOfWork.CommitAsync();

        // Ảnh vẫn được field còn lại dùng thì giữ lại.
        if (replacedIssueImage != issue.ConfirmImage) await _fileStorage.TryDeleteAsync(replacedIssueImage);
        if (replacedConfirmImage != issue.IssueImage) await _fileStorage.TryDeleteAsync(replacedConfirmImage);

        return IssueResponse.From(issue);
    }

    public async Task<IssueResponse> UpdateStatusAsync(Guid id, UpdateIssueStatusRequest request)
    {
        if (!Enum.TryParse<IssueStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: open, in_progress, resolved, closed.");

        var issue = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.IssueType))
            ?? throw new KeyNotFoundException($"Không tìm thấy issue với id {id}.");

        // open → in_progress → resolved → closed (chỉ tiến, không lùi).
        var allowed = issue.Status switch
        {
            IssueStatus.open => target == IssueStatus.in_progress,
            IssueStatus.in_progress => target == IssueStatus.resolved,
            IssueStatus.resolved => target == IssueStatus.closed,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"Không thể chuyển issue từ '{issue.Status}' sang '{target}'.");

        issue.Status = target;
        if (target == IssueStatus.resolved && issue.ActualAt == null)
            issue.ActualAt = DateOnly.FromDateTime(DateTime.UtcNow);
        issue.UpdatedAt = DateTime.UtcNow;

        _repository.Update(issue);
        await _unitOfWork.CommitAsync();

        return IssueResponse.From(issue);
    }

    public async Task DeleteAsync(Guid id)
    {
        var issue = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy issue với id {id}.");

        _repository.Delete(issue);
        await _unitOfWork.CommitAsync();

        // Dọn ảnh trên bucket sau khi DB đã commit (2 field có thể trỏ cùng 1 object).
        await _fileStorage.TryDeleteAsync(issue.IssueImage);
        if (issue.ConfirmImage != issue.IssueImage)
            await _fileStorage.TryDeleteAsync(issue.ConfirmImage);
    }
}
