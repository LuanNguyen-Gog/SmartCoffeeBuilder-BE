using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;
using ServiceProviderProfileEntity = SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile;
using AccountEntity = SmartCoffeeBuilder.Repository.Models.Account;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ServiceProviderProfileService : IServiceProviderProfileService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ServiceProviderProfileEntity> _repository;

    public ServiceProviderProfileService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ServiceProviderProfileEntity>();
    }

    public async Task<PaginationResponse<ServiceProviderProfileResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        string? capability = null, bool? isVerified = null, string? search = null)
    {
        Capability? cap = null;
        if (!string.IsNullOrWhiteSpace(capability))
        {
            if (!Enum.TryParse<Capability>(capability, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Capability '{capability}' is not valid. Allowed: designer, constructor, both.");
            cap = parsed;
        }

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var paged = await _repository
            .GetQueryable(p => p.DeletedAt == null
                // Lọc designer/constructor luôn gồm cả provider làm được cả hai.
                && (cap == null || p.Capability == cap || p.Capability == Capability.both)
                && (isVerified == null || p.IsVerified == isVerified)
                && (term == null || EF.Functions.ILike(p.DisplayName, $"%{term}%")))
            .OrderByDescending(p => p.AvgRating)
            .ThenByDescending(p => p.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ServiceProviderProfileResponse>(
            paged.Items.Select(ServiceProviderProfileResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ServiceProviderProfileResponse> GetByIdAsync(Guid id)
    {
        var provider = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"No service provider found with id {id}.");

        // Đường đơn lẻ thì FE render trang profile — nhúng dimension averages luôn để khỏi phải
        // gọi thêm /api/reviews/providers/{id}/summary. GetAllAsync bỏ qua bước này để list giữ gọn.
        var response = ServiceProviderProfileResponse.From(provider);
        response.DimensionAverages = (await LoadDimensionAveragesAsync(id)).DimensionAverages;
        return response;
    }

    public async Task<ServiceProviderProfileResponse> CreateAsync(
        Guid accountId, CreateServiceProviderProfileRequest request)
    {
        // Như ShopOwnerService.CreateAsync: giữ request.AccountId cho hợp đồng API, nhưng chỉ
        // nhận khi trùng token. Hồ sơ provider mang capability + verified — để client tự khai
        // account là mở đường dựng hồ sơ trên tài khoản chưa onboarding của người khác.
        if (request.AccountId != accountId
            && !await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId))
            throw new UnauthorizedAccessException(
                "A service provider profile can only be created for the signed-in account.");

        var account = await _unitOfWork.GetRepository<AccountEntity>()
            .SingleOrDefaultAsync(predicate: a => a.Id == request.AccountId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"No account found with id {request.AccountId}.");

        if (account.Role != AccountRole.provider)
            throw new ArgumentException("The account must have role 'provider' to create a service provider profile.");

        if (!Enum.TryParse<ProviderType>(request.ProviderType, ignoreCase: true, out var providerType))
            throw new ArgumentException($"ProviderType '{request.ProviderType}' is not valid. Allowed: individual, company.");

        if (!Enum.TryParse<Capability>(request.Capability, ignoreCase: true, out var capability))
            throw new ArgumentException($"Capability '{request.Capability}' is not valid. Allowed: designer, constructor, both.");

        var existing = await _repository.SingleOrDefaultAsync(predicate: p => p.AccountId == request.AccountId);
        if (existing != null)
            throw new InvalidOperationException("This account already has a service provider profile.");

        var provider = new ServiceProviderProfileEntity
        {
            AccountId = request.AccountId,
            DisplayName = request.DisplayName,
            ProviderType = providerType,
            Capability = capability,
            Bio = request.Bio,
            CompanyTaxCode = request.CompanyTaxCode,
            YearsExperience = request.YearsExperience,
            PortfolioHeadline = request.PortfolioHeadline,
            IsVerified = false,
            AvgRating = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(provider);
        await _unitOfWork.CommitAsync();

        return ServiceProviderProfileResponse.From(provider);
    }

    public async Task<ServiceProviderProfileResponse> UpdateAsync(
        Guid accountId, Guid id, UpdateServiceProviderProfileRequest request)
    {
        var provider = await _repository.GetByIdAsync(id);
        if (provider == null || provider.DeletedAt != null)
            throw new KeyNotFoundException($"No service provider found with id {id}.");

        // Một lần hỏi role, dùng cho cả hai check bên dưới.
        var isAdmin = await ResourceOwnership.IsAdminAsync(_unitOfWork, accountId);
        if (!isAdmin && provider.AccountId != accountId)
            throw new UnauthorizedAccessException(
                "This service provider profile belongs to another account — only its owner may edit it.");

        if (request.DisplayName != null) provider.DisplayName = request.DisplayName;

        if (!string.IsNullOrWhiteSpace(request.ProviderType))
        {
            if (!Enum.TryParse<ProviderType>(request.ProviderType, ignoreCase: true, out var providerType))
                throw new ArgumentException($"ProviderType '{request.ProviderType}' is not valid. Allowed: individual, company.");
            provider.ProviderType = providerType;
        }

        if (!string.IsNullOrWhiteSpace(request.Capability))
        {
            if (!Enum.TryParse<Capability>(request.Capability, ignoreCase: true, out var capability))
                throw new ArgumentException($"Capability '{request.Capability}' is not valid. Allowed: designer, constructor, both.");
            provider.Capability = capability;
        }

        if (request.Bio != null) provider.Bio = request.Bio;
        if (request.CompanyTaxCode != null) provider.CompanyTaxCode = request.CompanyTaxCode;
        if (request.YearsExperience.HasValue) provider.YearsExperience = request.YearsExperience;
        if (request.PortfolioHeadline != null) provider.PortfolioHeadline = request.PortfolioHeadline;

        // Cờ verified là dấu admin đã duyệt chứng chỉ — provider tự bật được thì huy hiệu
        // vô nghĩa. Bỏ qua im lặng sẽ khiến FE tưởng đã lưu, nên báo lỗi thẳng.
        if (request.IsVerified.HasValue)
        {
            if (!isAdmin)
                throw new UnauthorizedAccessException(
                    "Only an admin may change the verified flag of a provider profile.");
            provider.IsVerified = request.IsVerified.Value;
        }

        provider.UpdatedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();

        return ServiceProviderProfileResponse.From(provider);
    }

    public async Task DeleteAsync(Guid id)
    {
        var provider = await _repository.GetByIdAsync(id);
        if (provider == null || provider.DeletedAt != null)
            throw new KeyNotFoundException($"No service provider found with id {id}.");

        var activeEngagements = await _unitOfWork.GetRepository<SmartCoffeeBuilder.Repository.Models.ProjectWorking>()
            .CountAsync(e => e.ServiceProviderProfileId == id
                             && (e.Status == ProviderStatus.requested || e.Status == ProviderStatus.accepted));
        if (activeEngagements > 0)
            throw new InvalidOperationException(
                $"This provider still has {activeEngagements} active engagement(s) — close or cancel all of them before deleting the profile.");

        provider.DeletedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Lấy reviews của provider qua ProjectWorking để gộp dimension averages. Chỉ gọi từ đường
    /// đơn lẻ (<see cref="GetByIdAsync"/>) — GetAllAsync bỏ qua để list không tốn query thêm.
    /// </summary>
    private async Task<ProviderRatingAggregate> LoadDimensionAveragesAsync(Guid serviceProviderProfileId)
    {
        var reviews = await _unitOfWork.GetRepository<Repository.Models.Review>().GetListAsync(
            predicate: r => r.ProjectWorking.ServiceProviderProfileId == serviceProviderProfileId,
            include: q => q.Include(r => r.ReviewScores));

        return ProviderRatingAggregator.Aggregate(reviews);
    }
}
