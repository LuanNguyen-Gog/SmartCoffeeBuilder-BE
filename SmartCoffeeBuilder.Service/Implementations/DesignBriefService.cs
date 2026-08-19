using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
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

    public async Task<PaginationResponse<DesignBriefResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10, Guid? projectShopOwnerId = null)
    {
        // projectShopOwnerId là bộ lọc TIỆN LỢI, không phải hàng rào: bỏ trống thì trước đây
        // predicate đúng với mọi dòng và endpoint trả về brief của toàn hệ thống. Quyền xem phải
        // nằm ngay trong query (xem ProjectVisible bên dưới) — lọc sau khi lấy về sẽ làm sai
        // TotalItems của phân trang.
        var isAdmin = await IsAdminAsync(accountId);

        var query = _repository
            .GetQueryable(b => (projectShopOwnerId == null || b.ProjectShopOwnerId == projectShopOwnerId)
                               && b.ProjectShopOwner.DeletedAt == null
                               && (isAdmin
                                   || b.ProjectShopOwner.Owner.AccountId == accountId
                                   || b.ProjectShopOwner.ProjectWorkings.Any(
                                       e => e.ServiceProviderProfile.AccountId == accountId
                                            && e.Status != ProviderStatus.rejected
                                            && e.Status != ProviderStatus.terminated)
                                   || b.ProjectShopOwner.Posts.Any(p => p.Status == PostStatus.open)))
            .OrderByDescending(b => b.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DesignBriefResponse>(
            paged.Items.Select(DesignBriefResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DesignBriefResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var brief = await _repository.SingleOrDefaultAsync(predicate: b => b.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {id}.");

        await EnsureProjectVisibleAsync(accountId, brief.ProjectShopOwnerId);

        return DesignBriefResponse.From(brief);
    }

    public async Task<DesignBriefResponse> CreateAsync(Guid accountId, CreateDesignBriefRequest request)
    {
        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(
                predicate: p => p.Id == request.ProjectShopOwnerId && p.DeletedAt == null,
                include: q => q.Include(p => p.Owner))
            ?? throw new KeyNotFoundException($"Không tìm thấy project với id {request.ProjectShopOwnerId}.");

        // Quyền TRƯỚC check trùng: nếu check trùng chạy trước, người ngoài dò được dự án nào đã có
        // brief (409) và dự án nào chưa (401) — endpoint thành công cụ do thám. Ngoài ra bảng có
        // unique index trên project_id nên tạo hộ brief cho dự án người khác là chiếm luôn chỗ,
        // chủ thật sau đó không tạo được nữa.
        await EnsureOwnerAsync(accountId, project, "tạo brief cho dự án này");

        // DB có unique index trên project_id — check trước để trả 409 thay vì 500.
        if (await _repository.CountAsync(b => b.ProjectShopOwnerId == request.ProjectShopOwnerId) > 0)
            throw new InvalidOperationException($"ProjectShopOwner {request.ProjectShopOwnerId} đã có design brief.");

        var brief = new DesignBrief
        {
            ProjectShopOwnerId = request.ProjectShopOwnerId,
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

    public async Task<DesignBriefResponse> UpdateAsync(Guid accountId, Guid id, UpdateDesignBriefRequest request)
    {
        var brief = await LoadWithOwnerAsync(id);
        await EnsureOwnerAsync(accountId, brief.ProjectShopOwner, "sửa brief này");

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

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var brief = await LoadWithOwnerAsync(id);
        await EnsureOwnerAsync(accountId, brief.ProjectShopOwner, "xoá brief này");

        _repository.Delete(brief);
        await _unitOfWork.CommitAsync();
    }

    // ───────── Phân quyền theo dự án mang brief ─────────

    private async Task<DesignBrief> LoadWithOwnerAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: b => b.Id == id,
            include: q => q.Include(b => b.ProjectShopOwner).ThenInclude(p => p.Owner))
        ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {id}.");

    /// <summary>
    /// Ai được ĐỌC brief của một dự án:
    /// <list type="bullet">
    /// <item>chủ dự án — brief là của họ;</item>
    /// <item>provider có engagement trên dự án và engagement còn hiệu lực (khớp
    /// <c>ProjectWorkingService.EnsureEngagementViewable</c>: rejected/terminated thì hết quyền) —
    /// mở từ lúc 'requested' để provider quyết định nhận việc;</item>
    /// <item>mọi provider khi dự án còn bài đăng 'open' — bài đăng là lời mời thầu công khai,
    /// không đọc được yêu cầu thì không nộp hồ sơ được;</item>
    /// <item>admin.</item>
    /// </list>
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không liên quan tới dự án (HTTP 401).</exception>
    private async Task EnsureProjectVisibleAsync(Guid accountId, Guid projectShopOwnerId)
    {
        var visible = await _unitOfWork.GetRepository<ProjectShopOwner>().CountAsync(
            p => p.Id == projectShopOwnerId
                 && p.DeletedAt == null
                 && (p.Owner.AccountId == accountId
                     || p.ProjectWorkings.Any(e => e.ServiceProviderProfile.AccountId == accountId
                                                   && e.Status != ProviderStatus.rejected
                                                   && e.Status != ProviderStatus.terminated)
                     || p.Posts.Any(post => post.Status == PostStatus.open))) > 0;

        if (visible || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            "Brief này thuộc một dự án mà tài khoản đang đăng nhập không tham gia.");
    }

    /// <summary>
    /// Chỉ chủ dự án (hoặc admin) mới GHI được lên brief. Role gate <c>owner,admin</c> ở controller
    /// không thay được check này: mọi owner đều mang role 'owner' nên role gate cho qua tất.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không phải chủ dự án (HTTP 401).</exception>
    private async Task EnsureOwnerAsync(Guid accountId, ProjectShopOwner project, string action)
    {
        if (project.Owner?.AccountId == accountId) return;
        if (await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException($"Chỉ chủ dự án mới được {action}.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }
}
