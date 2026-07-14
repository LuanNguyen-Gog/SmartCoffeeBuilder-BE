using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectApplication;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectApplication;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ProjectApplicationService : IProjectApplicationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ProjectApplication> _repository;

    public ProjectApplicationService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ProjectApplication>();
    }

    public async Task<PaginationResponse<ProjectApplicationResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? postId = null, long? providerId = null, string? status = null)
    {
        ApplicationStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, accepted, rejected.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(
                a => (postId == null || a.PostId == postId)
                     && (providerId == null || a.ProviderId == providerId)
                     && (st == null || a.Status == st),
                include: q => q.Include(a => a.Post).Include(a => a.Provider))
            .OrderByDescending(a => a.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProjectApplicationResponse>(
            paged.Items.Select(ProjectApplicationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProjectApplicationResponse> GetByIdAsync(long id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        return ProjectApplicationResponse.From(application);
    }

    public async Task<ProjectApplicationResponse> ApplyAsync(CreateProjectApplicationRequest request)
    {
        var post = await _unitOfWork.GetRepository<ProjectPost>()
            .SingleOrDefaultAsync(predicate: p => p.Id == request.PostId)
            ?? throw new KeyNotFoundException($"Không tìm thấy bài đăng với id {request.PostId}.");

        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"Bài đăng đang ở trạng thái '{post.Status}', không nhận hồ sơ.");

        if (post.SubmissionDeadline.HasValue && post.SubmissionDeadline.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("Bài đăng đã quá hạn nộp hồ sơ.");

        var provider = await _unitOfWork.GetRepository<ServiceProvider>()
            .SingleOrDefaultAsync(predicate: s => s.Id == request.ProviderId && s.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {request.ProviderId}.");

        // Capability phải phù hợp service_kind của bài đăng (designer/constructor/both).
        var capabilityMatches = provider.Capability == Capability.both
            || (post.ServiceKind == ServiceKind.design && provider.Capability == Capability.designer)
            || (post.ServiceKind == ServiceKind.construction && provider.Capability == Capability.constructor);
        if (!capabilityMatches)
            throw new InvalidOperationException(
                $"Provider capability '{provider.Capability}' không phù hợp với bài đăng service kind '{post.ServiceKind}'.");

        var alreadyApplied = await _repository.CountAsync(
            a => a.PostId == post.Id && a.ProviderId == provider.Id && a.Status != ApplicationStatus.rejected) > 0;
        if (alreadyApplied)
            throw new InvalidOperationException("Provider đã nộp hồ sơ cho bài đăng này.");

        var application = new ProjectApplication
        {
            PostId = post.Id,
            ProviderId = provider.Id,
            Proposal = request.Proposal,
            EstimatedDurationDays = request.EstimatedDurationDays,
            Status = ApplicationStatus.pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(application);
        await _unitOfWork.CommitAsync();

        application.Post = post;
        application.Provider = provider;
        return ProjectApplicationResponse.From(application);
    }

    public async Task<ProjectApplicationResponse> UpdateProposalAsync(long id, UpdateProjectApplicationRequest request)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Application đang ở trạng thái '{application.Status}', chỉ sửa được khi pending.");

        if (request.Proposal != null) application.Proposal = request.Proposal;
        if (request.EstimatedDurationDays.HasValue) application.EstimatedDurationDays = request.EstimatedDurationDays;

        application.UpdatedAt = DateTime.UtcNow;
        _repository.Update(application);
        await _unitOfWork.CommitAsync();

        return ProjectApplicationResponse.From(application);
    }

    public async Task<ProjectProviderResponse> AcceptAsync(long id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).ThenInclude(p => p.Project).Include(a => a.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Application đang ở trạng thái '{application.Status}', chỉ chấp nhận được khi pending.");

        var post = application.Post;
        if (post.Status != PostStatus.open)
            throw new InvalidOperationException($"Bài đăng đang ở trạng thái '{post.Status}', không thể chấp nhận hồ sơ.");

        var now = DateTime.UtcNow;

        // 1. Chấp nhận hồ sơ này
        application.Status = ApplicationStatus.accepted;
        application.UpdatedAt = now;
        _repository.Update(application);

        // 2. Tạo engagement (đường marketplace: application_id có giá trị)
        var engagement = new ProjectProvider
        {
            ProjectId = post.ProjectId,
            ProviderId = application.ProviderId,
            ApplicationId = application.Id,
            ContractType = post.ServiceKind,
            Status = ProviderStatus.accepted,
            RequestMessage = $"Chấp nhận hồ sơ ứng tuyển #{application.Id} cho bài đăng \"{post.Title}\".",
            CreatedAt = now,
            UpdatedAt = now
        };
        await _unitOfWork.GetRepository<ProjectProvider>().InsertAsync(engagement);

        // 3. Đóng bài đăng
        post.Status = PostStatus.closed;
        post.UpdatedAt = now;
        _unitOfWork.GetRepository<ProjectPost>().Update(post);

        // 4. Từ chối các hồ sơ pending còn lại của bài đăng
        var otherPending = await _repository.GetListAsync(
            predicate: a => a.PostId == post.Id && a.Id != application.Id && a.Status == ApplicationStatus.pending);
        foreach (var other in otherPending)
        {
            other.Status = ApplicationStatus.rejected;
            other.UpdatedAt = now;
        }
        _repository.UpdateRange(otherPending);

        // SaveChanges chạy trong một transaction — 4 bước trên là atomic.
        await _unitOfWork.CommitAsync();

        engagement.Project = post.Project;
        engagement.Provider = application.Provider;
        return ProjectProviderResponse.From(engagement);
    }

    public async Task<ProjectApplicationResponse> RejectAsync(long id)
    {
        var application = await _repository.SingleOrDefaultAsync(
            predicate: a => a.Id == id,
            include: q => q.Include(a => a.Post).Include(a => a.Provider))
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Application đang ở trạng thái '{application.Status}', chỉ từ chối được khi pending.");

        application.Status = ApplicationStatus.rejected;
        application.UpdatedAt = DateTime.UtcNow;
        _repository.Update(application);
        await _unitOfWork.CommitAsync();

        return ProjectApplicationResponse.From(application);
    }

    public async Task WithdrawAsync(long id)
    {
        var application = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy application với id {id}.");

        if (application.Status != ApplicationStatus.pending)
            throw new InvalidOperationException($"Application đang ở trạng thái '{application.Status}', chỉ rút được khi pending.");

        _repository.Delete(application);
        await _unitOfWork.CommitAsync();
    }
}
