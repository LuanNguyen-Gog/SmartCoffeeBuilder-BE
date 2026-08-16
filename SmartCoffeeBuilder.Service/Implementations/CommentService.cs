using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Comment;
using SmartCoffeeBuilder.Service.DTOs.Responses.Comment;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class CommentService : ICommentService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Comment> _repository;

    public CommentService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Comment>();
    }

    public async Task<PaginationResponse<CommentResponse>> GetAllAsync(
        CommentTargetType targetType, Guid targetId,
        int pageNumber = 1, int pageSize = 20)
    {
        // Read mở — chỉ cần target tồn tại. CommentService không kiểm tra quyền xem vì thread public.
        var query = _repository
            .GetQueryable(
                c => c.TargetType == targetType && c.TargetId == targetId,
                include: q => q.Include(c => c.CreatedByAccount!)
                    .ThenInclude(a => a!.ShopOwner)
                    .Include(c => c.CreatedByAccount!)
                    .ThenInclude(a => a!.ServiceProviderProfile))
            .OrderByDescending(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<CommentResponse>(
            paged.Items.Select(CommentResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<CommentResponse> CreateAsync(CreateCommentRequest request, Guid currentAccountId)
    {
        // Ép CreatedBy = currentAccountId — không tin tưởng giá trị client gửi lên.
        request.CreatedBy = currentAccountId;

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ArgumentException("Nội dung comment không được để trống.");

        var targetType = ParseTargetType(request.TargetType);

        // 1. Lấy target + ProjectWorkingId tương ứng.
        var projectWorkingId = await ResolveProjectWorkingIdAsync(targetType, request.TargetId);

        // 2. Check quyền: account phải thuộc ProjectWorking hoặc là admin.
        await EnsureCanCommentAsync(projectWorkingId, currentAccountId);

        // 3. Load tên hiển thị người viết để FE render avatar — phải load trước khi insert
        //    vì sau khi save CreatedByAccount có thể chưa được Include.
        var author = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(
                predicate: a => a.Id == currentAccountId,
                include: q => q.Include(a => a.ShopOwner)
                    .Include(a => a.ServiceProviderProfile));

        var comment = new Comment
        {
            TargetType = targetType,
            TargetId = request.TargetId,
            Body = request.Body.Trim(),
            CreatedBy = currentAccountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(comment);
        await _unitOfWork.CommitAsync();

        // Gắn author để From() lấy được CreatedByName.
        comment.CreatedByAccount = author;
        return CommentResponse.From(comment);
    }

    public async Task DeleteAsync(Guid id, Guid currentAccountId)
    {
        var comment = await _repository.SingleOrDefaultAsync(
            predicate: c => c.Id == id,
            include: q => q.Include(c => c.CreatedByAccount))
            ?? throw new KeyNotFoundException($"Không tìm thấy comment với id {id}.");

        // Chỉ người tạo hoặc admin mới xoá.
        var current = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == currentAccountId)
            ?? throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");

        if (comment.CreatedBy != currentAccountId && current.Role != AccountRole.admin)
            throw new UnauthorizedAccessException("Chỉ người viết hoặc admin mới được xoá comment.");

        _repository.Delete(comment);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>Parse string TargetType (snake_case) từ request — chấp nhận cả PascalCase cho dễ FE.</summary>
    private static CommentTargetType ParseTargetType(string raw)
    {
        var normalized = raw.Trim().ToLowerInvariant().Replace("-", "_");
        if (!Enum.TryParse<CommentTargetType>(normalized, ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"TargetType '{raw}' không hợp lệ. Cho phép: construction_item, design.");
        return parsed;
    }

    /// <summary>Từ (target_type, target_id) suy ra ProjectWorkingId — dùng để check quyền.</summary>
    private async Task<Guid> ResolveProjectWorkingIdAsync(CommentTargetType type, Guid targetId)
    {
        switch (type)
        {
            case CommentTargetType.construction_item:
            {
                var item = await _unitOfWork.GetRepository<ConstructionItem>()
                    .SingleOrDefaultAsync(predicate: ci => ci.Id == targetId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {targetId}.");
                return item.ProjectWorkingId;
            }
            case CommentTargetType.design:
            {
                var design = await _unitOfWork.GetRepository<Design>()
                    .SingleOrDefaultAsync(predicate: d => d.Id == targetId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy design với id {targetId}.");
                return design.ProjectWorkingId;
            }
            default:
                throw new ArgumentException($"TargetType '{type}' chưa được hỗ trợ.");
        }
    }

    /// <summary>
    /// Đảm bảo currentAccountId có quyền comment trên ProjectWorking (owner/provider liên quan hoặc admin).
    /// Ném <see cref="UnauthorizedAccessException"/> nếu không thuộc.
    /// </summary>
    private async Task EnsureCanCommentAsync(Guid projectWorkingId, Guid currentAccountId)
    {
        var current = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(
                predicate: a => a.Id == currentAccountId,
                include: q => q.Include(a => a.ShopOwner).Include(a => a.ServiceProviderProfile))
            ?? throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");

        if (current.Role == AccountRole.admin) return;

        var pw = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(
                predicate: p => p.Id == projectWorkingId,
                include: q => q.Include(p => p.ProjectShopOwner).ThenInclude(ps => ps.Owner)
                    .Include(p => p.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"Không tìm thấy engagement với id {projectWorkingId}.");

        var isOwner = pw.ProjectShopOwner?.Owner?.AccountId == currentAccountId;
        var isProvider = pw.ServiceProviderProfile?.AccountId == currentAccountId;

        if (!isOwner && !isProvider)
            throw new UnauthorizedAccessException(
                "Bạn không thuộc engagement này — không thể comment.");
    }
}