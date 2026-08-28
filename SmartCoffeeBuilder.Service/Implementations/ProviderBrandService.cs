using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderBrand;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProviderBrand;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Thương hiệu và năng lực nhà cung cấp (review 1.1: "mở rộng hồ sơ provider bằng … video, năng
/// lực, thương hiệu").
///
/// ⚠️ Vì sao đây là service RIÊNG chứ không nhét vào <c>ServiceProviderProfileService.UpdateAsync</c>:
/// endpoint đó hiện chỉ có role gate <c>[Authorize(Roles="provider,admin")]</c> và KHÔNG check
/// ownership, nên bất kỳ provider nào cũng sửa được hồ sơ của provider khác (IDOR — xem mục
/// Authorization trong CLAUDE.md). Thêm logo / brand story vào đó là mở rộng lỗ hổng sẵn có sang
/// phần nhận diện thương hiệu. Đường này bắt buộc qua <see cref="EnsureCanWriteAsync"/>.
/// </summary>
public class ProviderBrandService : IProviderBrandService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Entities.ServiceProviderProfile> _repository;
    private readonly IFileStorageService _fileStorage;

    public ProviderBrandService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Entities.ServiceProviderProfile>();
        _fileStorage = fileStorage;
    }

    public async Task<ProviderBrandResponse> GetAsync(Guid serviceProviderProfileId) =>
        ProviderBrandResponse.From(await LoadGraphAsync(serviceProviderProfileId));

    public async Task<ProviderBrandResponse> UpdateBrandAsync(
        Guid accountId, Guid serviceProviderProfileId, UpdateProviderBrandRequest request)
    {
        var provider = await LoadAsync(serviceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "edit this profile's brand information");

        if (request.FoundedYear is int year)
        {
            var thisYear = DateTime.UtcNow.Year;
            if (year < 1900 || year > thisYear)
                throw new ArgumentException($"The founding year must be between 1900 and {thisYear}.");
            provider.FoundedYear = year;
        }
        if (request.EmployeeCount is int count)
        {
            if (count <= 0) throw new ArgumentException("The headcount must be greater than 0.");
            provider.EmployeeCount = count;
        }

        if (request.Website != null) provider.Website = request.Website;
        if (request.BrandStory != null) provider.BrandStory = request.BrandStory;
        if (request.CompanyAddress != null) provider.CompanyAddress = request.CompanyAddress;

        if (request.ClearCompanyCoordinates)
        {
            if (request.CompanyLatitude.HasValue || request.CompanyLongitude.HasValue)
                throw new ArgumentException(
                    "ClearCompanyCoordinates cannot be combined with a new latitude/longitude — " +
                    "send one or the other.");

            provider.CompanyLatitude = null;
            provider.CompanyLongitude = null;
        }
        else if (request.CompanyLatitude.HasValue || request.CompanyLongitude.HasValue)
        {
            GeoCoordinates.EnsurePairValid(
                request.CompanyLatitude, request.CompanyLongitude, "the company address");
            provider.CompanyLatitude = request.CompanyLatitude;
            provider.CompanyLongitude = request.CompanyLongitude;
        }

        // File cũ bị thay thì dọn object trên bucket SAU khi DB commit.
        string? replacedLogo = null, replacedCover = null, replacedVideo = null;

        if (request.LogoUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.LogoUrl, "logoUrl");
            if (next != provider.LogoUrl) replacedLogo = provider.LogoUrl;
            provider.LogoUrl = next;
        }
        if (request.CoverImageUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.CoverImageUrl, "coverImageUrl");
            if (next != provider.CoverImageUrl) replacedCover = provider.CoverImageUrl;
            provider.CoverImageUrl = next;
        }
        if (request.IntroVideoUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.IntroVideoUrl, "introVideoUrl");
            if (next != provider.IntroVideoUrl) replacedVideo = provider.IntroVideoUrl;
            provider.IntroVideoUrl = next;
        }

        provider.UpdatedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();

        await TryDeleteFileAsync(replacedLogo);
        await TryDeleteFileAsync(replacedCover);
        await TryDeleteFileAsync(replacedVideo);

        return await GetAsync(provider.Id);
    }

    // ───────────────────────── Kênh thương hiệu ─────────────────────────

    public async Task<ProviderSocialLinkResponse> AddSocialLinkAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderSocialLinkRequest request)
    {
        var provider = await LoadAsync(serviceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "add a brand channel to this profile");

        var platform = ParsePlatform(request.Platform);
        EnsureUrlValid(request.Url);

        var repo = _unitOfWork.GetRepository<ProviderSocialLink>();

        // DB có unique index (provider, platform) — check trước để trả 409 thay vì 500.
        if (await repo.CountAsync(
                l => l.ServiceProviderProfileId == provider.Id && l.Platform == platform) > 0)
            throw new InvalidOperationException(
                $"This profile already lists the '{platform}' channel — edit the existing row instead of adding a duplicate.");

        var existing = await repo.GetListAsync(
            selector: l => l.SortOrder, predicate: l => l.ServiceProviderProfileId == provider.Id);

        var now = DateTime.UtcNow;
        var link = new ProviderSocialLink
        {
            ServiceProviderProfileId = provider.Id,
            Platform = platform,
            Url = request.Url.Trim(),
            Label = request.Label,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = now,
            UpdatedAt = now
        };

        await repo.InsertAsync(link);
        await _unitOfWork.CommitAsync();

        return ProviderSocialLinkResponse.From(link);
    }

    public async Task<ProviderSocialLinkResponse> UpdateSocialLinkAsync(
        Guid accountId, Guid linkId, ProviderSocialLinkRequest request)
    {
        var repo = _unitOfWork.GetRepository<ProviderSocialLink>();
        var link = await repo.SingleOrDefaultAsync(predicate: l => l.Id == linkId)
            ?? throw new KeyNotFoundException($"No brand channel found with id {linkId}.");

        var provider = await LoadAsync(link.ServiceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "edit this profile's brand channel");

        var platform = ParsePlatform(request.Platform);
        EnsureUrlValid(request.Url);

        if (platform != link.Platform
            && await repo.CountAsync(l => l.ServiceProviderProfileId == provider.Id
                                          && l.Platform == platform && l.Id != link.Id) > 0)
            throw new InvalidOperationException($"This profile already lists the '{platform}' channel.");

        link.Platform = platform;
        link.Url = request.Url.Trim();
        link.Label = request.Label;
        if (request.SortOrder.HasValue) link.SortOrder = request.SortOrder.Value;
        link.UpdatedAt = DateTime.UtcNow;

        repo.Update(link);
        await _unitOfWork.CommitAsync();

        return ProviderSocialLinkResponse.From(link);
    }

    public async Task RemoveSocialLinkAsync(Guid accountId, Guid linkId)
    {
        var repo = _unitOfWork.GetRepository<ProviderSocialLink>();
        var link = await repo.SingleOrDefaultAsync(predicate: l => l.Id == linkId)
            ?? throw new KeyNotFoundException($"No brand channel found with id {linkId}.");

        await EnsureCanWriteAsync(
            accountId, await LoadAsync(link.ServiceProviderProfileId),
            "delete this profile's brand channel");

        repo.Delete(link);
        await _unitOfWork.CommitAsync();
    }

    // ───────────────────────── Khu vực phục vụ ─────────────────────────

    public async Task<ProviderServiceAreaResponse> AddServiceAreaAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderServiceAreaRequest request)
    {
        var provider = await LoadAsync(serviceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "add a service area to this profile");

        if (string.IsNullOrWhiteSpace(request.Province))
            throw new ArgumentException("A service area must have a province or city.");

        var province = request.Province.Trim();
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();

        var repo = _unitOfWork.GetRepository<ProviderServiceArea>();
        if (await repo.CountAsync(a => a.ServiceProviderProfileId == provider.Id
                                       && a.Province == province && a.District == district) > 0)
            throw new InvalidOperationException(
                $"This profile already lists the '{district ?? province}' area.");

        var existing = await repo.GetListAsync(
            selector: a => a.SortOrder, predicate: a => a.ServiceProviderProfileId == provider.Id);

        var now = DateTime.UtcNow;
        var area = new ProviderServiceArea
        {
            ServiceProviderProfileId = provider.Id,
            Province = province,
            District = district,
            Note = request.Note,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = now,
            UpdatedAt = now
        };

        await repo.InsertAsync(area);
        await _unitOfWork.CommitAsync();

        return ProviderServiceAreaResponse.From(area);
    }

    public async Task RemoveServiceAreaAsync(Guid accountId, Guid areaId)
    {
        var repo = _unitOfWork.GetRepository<ProviderServiceArea>();
        var area = await repo.SingleOrDefaultAsync(predicate: a => a.Id == areaId)
            ?? throw new KeyNotFoundException($"No service area found with id {areaId}.");

        await EnsureCanWriteAsync(
            accountId, await LoadAsync(area.ServiceProviderProfileId),
            "delete this profile's service area");

        repo.Delete(area);
        await _unitOfWork.CommitAsync();
    }

    // ───────────────────────── Giấy phép / chứng chỉ ─────────────────────────

    public async Task<ProviderCertificateResponse> AddCertificateAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderCertificateRequest request)
    {
        var provider = await LoadAsync(serviceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "add a certificate to this profile");

        EnsureCertificateValid(request);

        var repo = _unitOfWork.GetRepository<ProviderCertificate>();
        var existing = await repo.GetListAsync(
            selector: c => c.SortOrder, predicate: c => c.ServiceProviderProfileId == provider.Id);

        var now = DateTime.UtcNow;
        var certificate = new ProviderCertificate
        {
            ServiceProviderProfileId = provider.Id,
            Kind = ParseKind(request.Kind),
            Name = request.Name.Trim(),
            Issuer = request.Issuer,
            CertificateNo = request.CertificateNo,
            IssuedAt = request.IssuedAt,
            ExpiresAt = request.ExpiresAt,
            FileUrl = await _fileStorage.NormalizeForStorageAsync(request.FileUrl, "fileUrl"),

            // Provider tự khai thì LUÔN false — chỉ admin bật được qua VerifyCertificateAsync.
            IsVerified = false,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedAt = now,
            UpdatedAt = now
        };

        await repo.InsertAsync(certificate);
        await _unitOfWork.CommitAsync();

        return ProviderCertificateResponse.From(certificate);
    }

    public async Task<ProviderCertificateResponse> UpdateCertificateAsync(
        Guid accountId, Guid certificateId, ProviderCertificateRequest request)
    {
        var repo = _unitOfWork.GetRepository<ProviderCertificate>();
        var certificate = await repo.SingleOrDefaultAsync(predicate: c => c.Id == certificateId)
            ?? throw new KeyNotFoundException($"No certificate found with id {certificateId}.");

        var provider = await LoadAsync(certificate.ServiceProviderProfileId);
        await EnsureCanWriteAsync(accountId, provider, "edit this profile's certificate");

        EnsureCertificateValid(request);

        string? replacedFile = null;
        if (request.FileUrl != null)
        {
            var next = await _fileStorage.NormalizeForStorageAsync(request.FileUrl, "fileUrl");
            if (next != certificate.FileUrl) replacedFile = certificate.FileUrl;
            certificate.FileUrl = next;
        }

        certificate.Kind = ParseKind(request.Kind);
        certificate.Name = request.Name.Trim();
        certificate.Issuer = request.Issuer;
        certificate.CertificateNo = request.CertificateNo;
        certificate.IssuedAt = request.IssuedAt;
        certificate.ExpiresAt = request.ExpiresAt;
        if (request.SortOrder.HasValue) certificate.SortOrder = request.SortOrder.Value;

        // Sửa nội dung giấy tờ thì xác minh cũ hết giá trị — admin phải đối chiếu lại bản mới,
        // nếu không thì provider up giấy tờ thật, được duyệt, rồi thay bằng giấy khác.
        certificate.IsVerified = false;
        certificate.UpdatedAt = DateTime.UtcNow;

        repo.Update(certificate);
        await _unitOfWork.CommitAsync();

        await TryDeleteFileAsync(replacedFile);

        return ProviderCertificateResponse.From(certificate);
    }

    public async Task RemoveCertificateAsync(Guid accountId, Guid certificateId)
    {
        var repo = _unitOfWork.GetRepository<ProviderCertificate>();
        var certificate = await repo.SingleOrDefaultAsync(predicate: c => c.Id == certificateId)
            ?? throw new KeyNotFoundException($"No certificate found with id {certificateId}.");

        await EnsureCanWriteAsync(
            accountId, await LoadAsync(certificate.ServiceProviderProfileId),
            "delete this profile's certificate");

        var objectName = certificate.FileUrl;

        repo.Delete(certificate);
        await _unitOfWork.CommitAsync();

        await TryDeleteFileAsync(objectName);
    }

    public async Task<ProviderCertificateResponse> VerifyCertificateAsync(
        Guid accountId, Guid certificateId, bool isVerified)
    {
        var repo = _unitOfWork.GetRepository<ProviderCertificate>();
        var certificate = await repo.SingleOrDefaultAsync(predicate: c => c.Id == certificateId)
            ?? throw new KeyNotFoundException($"No certificate found with id {certificateId}.");

        // CHỈ admin — provider tự xác minh giấy tờ của mình thì cờ này vô nghĩa.
        if (!await IsAdminAsync(accountId))
            throw new UnauthorizedAccessException("Only an admin can verify credential documents.");

        certificate.IsVerified = isVerified;
        certificate.UpdatedAt = DateTime.UtcNow;

        repo.Update(certificate);
        await _unitOfWork.CommitAsync();

        return ProviderCertificateResponse.From(certificate);
    }

    // ───────────────────────── Helper ─────────────────────────

    private async Task<Entities.ServiceProviderProfile> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id && p.DeletedAt == null)
        ?? throw new KeyNotFoundException($"No service provider found with id {id}.");

    private async Task<Entities.ServiceProviderProfile> LoadGraphAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: p => p.Id == id && p.DeletedAt == null,
            include: q => q.Include(p => p.SocialLinks)
                           .Include(p => p.ServiceAreas)
                           .Include(p => p.Certificates))
        ?? throw new KeyNotFoundException($"No service provider found with id {id}.");

    /// <summary>
    /// Chỉ chính chủ hồ sơ (hoặc admin) mới ghi được. Role gate ở controller KHÔNG thay được check
    /// này: mọi provider đều mang role 'provider' nên role gate cho qua tất.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không sở hữu hồ sơ (HTTP 401).</exception>
    private async Task EnsureCanWriteAsync(
        Guid accountId, Entities.ServiceProviderProfile provider, string action)
    {
        if (provider.AccountId == accountId) return;
        if (await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException($"Only the provider who owns this profile may {action}.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    /// <summary>Dọn file mồ côi — best-effort, hỏng thì kệ: bản ghi đã ghi xong rồi.</summary>
    private async Task TryDeleteFileAsync(string? objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return;

        try { await _fileStorage.TryDeleteAsync(objectName); }
        catch { /* rác trên bucket không đáng để làm hỏng một request đã thành công */ }
    }

    private static void EnsureCertificateValid(ProviderCertificateRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name))
            throw new ArgumentException("A certificate must have a name.");

        if (r.IssuedAt is DateOnly issued && r.ExpiresAt is DateOnly expires && expires < issued)
            throw new ArgumentException(
                $"The expiry date '{expires:yyyy-MM-dd}' falls before the issue date '{issued:yyyy-MM-dd}'.");
    }

    /// <summary>
    /// URL phải là http/https tuyệt đối — chuỗi kiểu "fanpage của tôi" lọt xuống DB thì FE render
    /// ra một link chết trên hồ sơ công khai.
    /// </summary>
    /// <exception cref="ArgumentException">Không phải URL http/https hợp lệ (HTTP 400).</exception>
    private static void EnsureUrlValid(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("A brand channel must have a URL.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException($"URL '{url}' is not valid — it must start with http:// or https://.");
    }

    private static SocialPlatform ParsePlatform(string raw)
    {
        if (Enum.TryParse<SocialPlatform>((raw ?? string.Empty).Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException(
            $"Platform '{raw}' is not valid. Accepted: {string.Join(", ", Enum.GetNames<SocialPlatform>())}.");
    }

    private static CertificateKind ParseKind(string raw)
    {
        if (Enum.TryParse<CertificateKind>((raw ?? string.Empty).Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException(
            $"Document kind '{raw}' is not valid. Accepted: {string.Join(", ", Enum.GetNames<CertificateKind>())}.");
    }
}
