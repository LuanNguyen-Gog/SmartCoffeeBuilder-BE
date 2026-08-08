using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.Interfaces;
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
                throw new ArgumentException($"Capability '{capability}' không hợp lệ. Cho phép: designer, constructor, both.");
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

    public async Task<ServiceProviderProfileResponse> GetByIdAsync(long id)
    {
        var provider = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {id}.");

        return ServiceProviderProfileResponse.From(provider);
    }

    public async Task<ServiceProviderProfileResponse> CreateAsync(CreateServiceProviderProfileRequest request)
    {
        var account = await _unitOfWork.GetRepository<AccountEntity>()
            .SingleOrDefaultAsync(predicate: a => a.Id == request.AccountId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.AccountId}.");

        if (account.Role != AccountRole.provider)
            throw new ArgumentException("Account phải có role 'provider' để tạo service provider.");

        if (!Enum.TryParse<ProviderType>(request.ProviderType, ignoreCase: true, out var providerType))
            throw new ArgumentException($"ProviderType '{request.ProviderType}' không hợp lệ. Cho phép: individual, company.");

        if (!Enum.TryParse<Capability>(request.Capability, ignoreCase: true, out var capability))
            throw new ArgumentException($"Capability '{request.Capability}' không hợp lệ. Cho phép: designer, constructor, both.");

        var existing = await _repository.SingleOrDefaultAsync(predicate: p => p.AccountId == request.AccountId);
        if (existing != null)
            throw new InvalidOperationException("Account này đã có hồ sơ service provider.");

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

    public async Task<ServiceProviderProfileResponse> UpdateAsync(long id, UpdateServiceProviderProfileRequest request)
    {
        var provider = await _repository.GetByIdAsync(id);
        if (provider == null || provider.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy service provider với id {id}.");

        if (request.DisplayName != null) provider.DisplayName = request.DisplayName;

        if (!string.IsNullOrWhiteSpace(request.ProviderType))
        {
            if (!Enum.TryParse<ProviderType>(request.ProviderType, ignoreCase: true, out var providerType))
                throw new ArgumentException($"ProviderType '{request.ProviderType}' không hợp lệ. Cho phép: individual, company.");
            provider.ProviderType = providerType;
        }

        if (!string.IsNullOrWhiteSpace(request.Capability))
        {
            if (!Enum.TryParse<Capability>(request.Capability, ignoreCase: true, out var capability))
                throw new ArgumentException($"Capability '{request.Capability}' không hợp lệ. Cho phép: designer, constructor, both.");
            provider.Capability = capability;
        }

        if (request.Bio != null) provider.Bio = request.Bio;
        if (request.CompanyTaxCode != null) provider.CompanyTaxCode = request.CompanyTaxCode;
        if (request.YearsExperience.HasValue) provider.YearsExperience = request.YearsExperience;
        if (request.PortfolioHeadline != null) provider.PortfolioHeadline = request.PortfolioHeadline;
        if (request.IsVerified.HasValue) provider.IsVerified = request.IsVerified.Value;

        provider.UpdatedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();

        return ServiceProviderProfileResponse.From(provider);
    }

    public async Task DeleteAsync(long id)
    {
        var provider = await _repository.GetByIdAsync(id);
        if (provider == null || provider.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy service provider với id {id}.");

        var activeEngagements = await _unitOfWork.GetRepository<SmartCoffeeBuilder.Repository.Models.ProjectWorking>()
            .CountAsync(e => e.ServiceProviderProfileId == id
                             && (e.Status == ProviderStatus.requested || e.Status == ProviderStatus.accepted));
        if (activeEngagements > 0)
            throw new InvalidOperationException(
                $"Provider còn {activeEngagements} engagement đang hoạt động — đóng/huỷ hết trước khi xoá hồ sơ.");

        provider.DeletedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();
    }
}
