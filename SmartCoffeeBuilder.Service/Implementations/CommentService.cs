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
        CommentTargetType targetType, Guid targetId, Guid currentAccountId,
        int pageNumber = 1, int pageSize = 20)
    {
        // Thread của design/construction_item nằm sẵn trong một engagement nên vẫn để mở như cũ.
        // Thread BÁO GIÁ thì không: nhiều provider cùng nộp báo giá vào một bài đăng, để mở thì
        // đối thủ chỉ cần đoán id là đọc được cả cuộc mặc cả giá.
        if (targetType == CommentTargetType.quotation)
        {
            var parties = await ResolvePartiesAsync(targetType, targetId);
            await EnsureCanCommentAsync(parties, currentAccountId);
        }

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

        // 1. Từ target suy ra hai đầu account của chỗ neo (owner + provider).
        var parties = await ResolvePartiesAsync(targetType, request.TargetId);

        // 2. Check quyền: account phải là một trong hai bên, hoặc admin.
        await EnsureCanCommentAsync(parties, currentAccountId);

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
                $"TargetType '{raw}' không hợp lệ. Cho phép: {TargetTypeList}.");
        return parsed;
    }

    /// <summary>Danh sách giá trị hợp lệ, sinh từ enum để câu lỗi không lạc hậu khi thêm target mới.</summary>
    private static readonly string TargetTypeList = string.Join(", ", Enum.GetNames<CommentTargetType>());

    /// <summary>
    /// Hai đầu account của chỗ neo — người viết comment phải là một trong hai (hoặc admin).
    /// Dùng cặp account thay vì <c>ProjectWorkingId</c> vì báo giá gắn hồ sơ ứng tuyển CHƯA có
    /// engagement: lúc đó owner đến từ dự án của bài đăng, provider đến từ chính hồ sơ.
    /// </summary>
    private sealed record CommentParties(Guid OwnerAccountId, Guid ProviderAccountId);

    /// <summary>Từ (target_type, target_id) suy ra hai bên được phép trao đổi trên thread đó.</summary>
    private async Task<CommentParties> ResolvePartiesAsync(CommentTargetType type, Guid targetId)
    {
        switch (type)
        {
            case CommentTargetType.construction_item:
            {
                var item = await _unitOfWork.GetRepository<ConstructionItem>()
                    .SingleOrDefaultAsync(predicate: ci => ci.Id == targetId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {targetId}.");
                return await LoadEngagementPartiesAsync(item.ProjectWorkingId);
            }
            case CommentTargetType.design:
            {
                var design = await _unitOfWork.GetRepository<Design>()
                    .SingleOrDefaultAsync(predicate: d => d.Id == targetId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy design với id {targetId}.");
                return await LoadEngagementPartiesAsync(design.ProjectWorkingId);
            }
            case CommentTargetType.quotation:
            {
                var quotation = await _unitOfWork.GetRepository<Quotation>()
                    .SingleOrDefaultAsync(predicate: q => q.Id == targetId)
                    ?? throw new KeyNotFoundException($"Không tìm thấy báo giá với id {targetId}.");

                // CHECK ck_quotations_anchor bảo đảm đúng MỘT trong hai cột có giá trị.
                return quotation.ApplyId is Guid applyId
                    ? await LoadApplyPartiesAsync(applyId)
                    : await LoadEngagementPartiesAsync(quotation.ProjectWorkingId!.Value);
            }
            default:
                throw new ArgumentException($"TargetType '{type}' chưa được hỗ trợ.");
        }
    }

    /// <summary>Owner của dự án + provider của engagement, projection để khỏi nạp cả graph.</summary>
    private async Task<CommentParties> LoadEngagementPartiesAsync(Guid projectWorkingId) =>
        (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            selector: e => new CommentParties(
                e.ProjectShopOwner.Owner.AccountId,
                e.ServiceProviderProfile.AccountId),
            predicate: e => e.Id == projectWorkingId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"Không tìm thấy engagement với id {projectWorkingId}.");

    /// <summary>Owner đến từ dự án của bài đăng, provider đến từ chính hồ sơ ứng tuyển.</summary>
    private async Task<CommentParties> LoadApplyPartiesAsync(Guid applyId) =>
        (await _unitOfWork.GetRepository<Apply>().GetListAsync(
            selector: a => new CommentParties(
                a.Post.ProjectShopOwner.Owner.AccountId,
                a.ServiceProviderProfile.AccountId),
            predicate: a => a.Id == applyId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ứng tuyển với id {applyId}.");

    /// <summary>
    /// Đảm bảo currentAccountId là một trong hai bên của chỗ neo, hoặc admin.
    /// Ném <see cref="UnauthorizedAccessException"/> nếu không thuộc.
    /// </summary>
    private async Task EnsureCanCommentAsync(CommentParties parties, Guid currentAccountId)
    {
        if (parties.OwnerAccountId == currentAccountId) return;
        if (parties.ProviderAccountId == currentAccountId) return;

        var current = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == currentAccountId && a.DeletedAt == null)
            ?? throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");

        if (current.Role == AccountRole.admin) return;

        throw new UnauthorizedAccessException(
            "Bạn không thuộc hồ sơ/hợp tác này — không thể comment.");
    }
}