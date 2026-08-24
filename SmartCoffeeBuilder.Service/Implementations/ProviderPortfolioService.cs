using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderPortfolio;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProviderPortfolio;
using SmartCoffeeBuilder.Service.Interfaces;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Dự án mẫu của nhà cung cấp (review 1.1: "mở rộng hồ sơ provider bằng dự án mẫu, video…").
///
/// ĐỌC là công khai (chỉ cần đăng nhập): hồ sơ năng lực tồn tại để chủ quán so sánh các provider
/// TRƯỚC khi thuê — rào nó sau một engagement thì nó vô dụng. Đây cũng là lý do không dùng
/// <c>docs</c> cho việc này: docs neo vào <c>project_providers</c> nên phải có hợp tác rồi mới up
/// được file.
///
/// GHI thì chỉ chính provider sở hữu hồ sơ, hoặc admin.
/// </summary>
public class ProviderPortfolioService : IProviderPortfolioService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Entities.ProviderPortfolio> _repository;
    private readonly IFileStorageService _fileStorage;

    public ProviderPortfolioService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Entities.ProviderPortfolio>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<ProviderPortfolioResponse>> GetByProviderAsync(
        Guid serviceProviderProfileId, int pageNumber = 1, int pageSize = 20)
    {
        var query = _repository
            .GetQueryable(p => p.ServiceProviderProfileId == serviceProviderProfileId)
            .Include(p => p.Images)

            // Ghim lên đầu, rồi tới thứ tự provider sắp, rồi tới công trình mới nhất — provider
            // muốn khoe cái gì trước thì cái đó phải nằm trước.
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.SortOrder)
            .ThenByDescending(p => p.CompletedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ProviderPortfolioResponse>(
            paged.Items.Select(ProviderPortfolioResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ProviderPortfolioResponse> GetByIdAsync(Guid id) =>
        ProviderPortfolioResponse.From(await LoadGraphAsync(id));

    public async Task<ProviderPortfolioResponse> CreateAsync(
        Guid accountId, CreateProviderPortfolioRequest request)
    {
        var providerId = await ResolveProviderIdAsync(accountId, request.ServiceProviderProfileId);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Dự án mẫu phải có tiêu đề.");
        EnsureMetricsValid(request.AreaM2, request.ContractValue, request.DurationDays);

        var now = DateTime.UtcNow;
        var existing = await _repository.GetListAsync(
            selector: p => p.SortOrder, predicate: p => p.ServiceProviderProfileId == providerId);

        var portfolio = new Entities.ProviderPortfolio
        {
            ServiceProviderProfileId = providerId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Role = ParseRole(request.Role),
            Style = request.Style,
            Location = request.Location,
            AreaM2 = request.AreaM2,
            ContractValue = request.ContractValue,
            CompletedAt = request.CompletedAt,
            DurationDays = request.DurationDays,

            // Video có thể là link YouTube (giữ nguyên) hoặc file đã upload lên bucket (rút về
            // ObjectName) — NormalizeForStorageAsync xử lý đúng cả hai.
            VideoUrl = await _fileStorage.NormalizeForStorageAsync(request.VideoUrl, "videoUrl"),
            CoverImageUrl = await _fileStorage.NormalizeForStorageAsync(
                request.CoverImageUrl, "coverImageUrl"),
            IsFeatured = request.IsFeatured,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.InsertAsync(portfolio);

        // Id uuid do Postgres sinh: chỉ có giá trị SAU commit, nên ảnh phải gắn ở lượt thứ hai.
        await _unitOfWork.CommitAsync();

        if (request.Images is { Count: > 0 })
        {
            var order = 0;
            foreach (var img in request.Images)
            {
                await _unitOfWork.GetRepository<ProviderPortfolioImage>().InsertAsync(
                    new ProviderPortfolioImage
                    {
                        ProviderPortfolioId = portfolio.Id,
                        ImageUrl = await NormalizeImageAsync(img.ImageUrl),
                        Caption = img.Caption,
                        SortOrder = img.SortOrder ?? order++,
                        CreatedAt = now
                    });
            }
            await _unitOfWork.CommitAsync();
        }

        return await GetByIdAsync(portfolio.Id);
    }

    public async Task<ProviderPortfolioResponse> UpdateAsync(
        Guid accountId, Guid id, UpdateProviderPortfolioRequest request)
    {
        var portfolio = await LoadAsync(id);
        await EnsureCanWriteAsync(accountId, portfolio.ServiceProviderProfileId, "sửa dự án mẫu này");

        EnsureMetricsValid(request.AreaM2, request.ContractValue, request.DurationDays);

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Dự án mẫu phải có tiêu đề.");
            portfolio.Title = request.Title.Trim();
        }
        if (request.Description != null) portfolio.Description = request.Description;
        if (request.Role != null) portfolio.Role = ParseRole(request.Role);
        if (request.Style != null) portfolio.Style = request.Style;
        if (request.Location != null) portfolio.Location = request.Location;
        if (request.AreaM2.HasValue) portfolio.AreaM2 = request.AreaM2;
        if (request.ContractValue.HasValue) portfolio.ContractValue = request.ContractValue;
        if (request.CompletedAt.HasValue) portfolio.CompletedAt = request.CompletedAt;
        if (request.DurationDays.HasValue) portfolio.DurationDays = request.DurationDays;
        if (request.IsFeatured.HasValue) portfolio.IsFeatured = request.IsFeatured.Value;
        if (request.SortOrder.HasValue) portfolio.SortOrder = request.SortOrder.Value;

        // File cũ bị thay thì dọn object trên bucket SAU khi DB commit (giống ConstructionTaskService).
        string? replacedVideo = null;
        string? replacedCover = null;

        if (request.VideoUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.VideoUrl, "videoUrl");
            if (next != portfolio.VideoUrl) replacedVideo = portfolio.VideoUrl;
            portfolio.VideoUrl = next;
        }
        if (request.CoverImageUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.CoverImageUrl, "coverImageUrl");
            if (next != portfolio.CoverImageUrl) replacedCover = portfolio.CoverImageUrl;
            portfolio.CoverImageUrl = next;
        }

        portfolio.UpdatedAt = DateTime.UtcNow;
        _repository.Update(portfolio);
        await _unitOfWork.CommitAsync();

        await TryDeleteFileAsync(replacedVideo);
        await TryDeleteFileAsync(replacedCover);

        return await GetByIdAsync(portfolio.Id);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var portfolio = await LoadGraphAsync(id);
        await EnsureCanWriteAsync(accountId, portfolio.ServiceProviderProfileId, "xoá dự án mẫu này");

        // Gom file TRƯỚC khi xoá bản ghi — sau khi cascade chạy thì không còn đường tra ObjectName.
        var orphanFiles = portfolio.Images.Select(i => i.ImageUrl)
            .Append(portfolio.CoverImageUrl)
            .Append(portfolio.VideoUrl)
            .ToList();

        // Ảnh con cascade theo FK ở DB.
        _repository.Delete(portfolio);
        await _unitOfWork.CommitAsync();

        foreach (var file in orphanFiles) await TryDeleteFileAsync(file);
    }

    public async Task<ProviderPortfolioImageResponse> AddImageAsync(
        Guid accountId, Guid portfolioId, ProviderPortfolioImageRequest request)
    {
        var portfolio = await LoadAsync(portfolioId);
        await EnsureCanWriteAsync(
            accountId, portfolio.ServiceProviderProfileId, "thêm ảnh cho dự án mẫu này");

        var repo = _unitOfWork.GetRepository<ProviderPortfolioImage>();
        var existing = await repo.GetListAsync(
            selector: i => i.SortOrder, predicate: i => i.ProviderPortfolioId == portfolio.Id);

        var image = new ProviderPortfolioImage
        {
            ProviderPortfolioId = portfolio.Id,
            ImageUrl = await NormalizeImageAsync(request.ImageUrl),
            Caption = request.Caption,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = DateTime.UtcNow
        };

        await repo.InsertAsync(image);
        await _unitOfWork.CommitAsync();

        return ProviderPortfolioImageResponse.From(image);
    }

    public async Task RemoveImageAsync(Guid accountId, Guid imageId)
    {
        var repo = _unitOfWork.GetRepository<ProviderPortfolioImage>();
        var image = await repo.SingleOrDefaultAsync(
            predicate: i => i.Id == imageId,
            include: q => q.Include(i => i.ProviderPortfolio))
            ?? throw new KeyNotFoundException($"Không tìm thấy ảnh dự án mẫu với id {imageId}.");

        await EnsureCanWriteAsync(
            accountId, image.ProviderPortfolio.ServiceProviderProfileId, "xoá ảnh của dự án mẫu này");

        var objectName = image.ImageUrl;

        repo.Delete(image);
        await _unitOfWork.CommitAsync();

        await TryDeleteFileAsync(objectName);
    }

    // ───────────────────────── Helper ─────────────────────────

    private async Task<Entities.ProviderPortfolio> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id)
        ?? throw new KeyNotFoundException($"Không tìm thấy dự án mẫu với id {id}.");

    private async Task<Entities.ProviderPortfolio> LoadGraphAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id,
            include: q => q.Include(p => p.Images))
        ?? throw new KeyNotFoundException($"Không tìm thấy dự án mẫu với id {id}.");

    /// <summary>
    /// Hồ sơ provider mà dự án mẫu này thuộc về. Bỏ trống <paramref name="requested"/> = hồ sơ của
    /// chính tài khoản đang đăng nhập; điền tay thì phải là hồ sơ của mình, trừ admin.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Tài khoản chưa có hồ sơ provider (HTTP 404).</exception>
    /// <exception cref="UnauthorizedAccessException">Khai hộ hồ sơ người khác (HTTP 401).</exception>
    private async Task<Guid> ResolveProviderIdAsync(Guid accountId, Guid? requested)
    {
        var own = (await _unitOfWork.GetRepository<ServiceProviderProfile>().GetListAsync(
                selector: p => p.Id,
                predicate: p => p.AccountId == accountId && p.DeletedAt == null))
            .FirstOrDefault();

        if (requested is not Guid target)
            return own != Guid.Empty
                ? own
                : throw new KeyNotFoundException(
                    "Tài khoản này chưa có hồ sơ nhà cung cấp — tạo hồ sơ trước khi thêm dự án mẫu.");

        if (target == own) return target;
        if (await IsAdminAsync(accountId)) return target;

        throw new UnauthorizedAccessException(
            "Chỉ thêm được dự án mẫu vào hồ sơ nhà cung cấp của chính mình.");
    }

    /// <exception cref="UnauthorizedAccessException">Không sở hữu hồ sơ (HTTP 401).</exception>
    private async Task EnsureCanWriteAsync(Guid accountId, Guid providerId, string action)
    {
        var isOwnProfile = await _unitOfWork.GetRepository<ServiceProviderProfile>()
            .CountAsync(p => p.Id == providerId && p.AccountId == accountId && p.DeletedAt == null) > 0;

        if (isOwnProfile || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException($"Chỉ nhà cung cấp sở hữu hồ sơ mới được {action}.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    private async Task<string> NormalizeImageAsync(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Ảnh dự án mẫu phải có imageUrl.");

        return await _fileStorage.NormalizeForStorageAsync(raw, "imageUrl")
            ?? throw new ArgumentException("Ảnh dự án mẫu phải có imageUrl.");
    }

    /// <summary>Dọn file mồ côi — best-effort, hỏng thì kệ: bản ghi đã xoá xong rồi.</summary>
    private async Task TryDeleteFileAsync(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return;

        try { await _fileStorage.TryDeleteAsync(objectName); }
        catch { /* rác trên bucket không đáng để làm hỏng một request đã thành công */ }
    }

    private static void EnsureMetricsValid(decimal? areaM2, decimal? contractValue, int? durationDays)
    {
        if (areaM2 is decimal a && a <= 0)
            throw new ArgumentException("Diện tích công trình phải lớn hơn 0 — bỏ trống nếu không nhớ.");
        if (contractValue is decimal v && v < 0)
            throw new ArgumentException("Giá trị hợp đồng không được âm.");
        if (durationDays is int d && d <= 0)
            throw new ArgumentException("Số ngày thi công phải lớn hơn 0.");
    }

    private static ServiceKind ParseRole(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return ServiceKind.both;
        if (Enum.TryParse<ServiceKind>(raw.Trim(), ignoreCase: true, out var parsed)) return parsed;

        throw new ArgumentException(
            $"Vai trò '{raw}' không hợp lệ. Nhận: {string.Join(", ", Enum.GetNames<ServiceKind>())}.");
    }
}
