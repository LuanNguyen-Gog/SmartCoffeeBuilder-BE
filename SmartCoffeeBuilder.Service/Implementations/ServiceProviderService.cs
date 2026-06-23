using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests;
using SmartCoffeeBuilder.Service.DTOs.Responses;
using SmartCoffeeBuilder.Service.Interfaces;
using ServiceProviderEntity = SmartCoffeeBuilder.Repository.Models.ServiceProvider;
using AccountEntity = SmartCoffeeBuilder.Repository.Models.Account;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ServiceProviderService : IServiceProviderService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ServiceProviderEntity> _repository;

    public ServiceProviderService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ServiceProviderEntity>();
    }

    public async Task<PaginationResponse<ServiceProviderResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        var paged = await _repository
            .GetQueryable(p => p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ServiceProviderResponse>(
            paged.Items.Select(ServiceProviderResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ServiceProviderResponse> GetByIdAsync(long id)
    {
        var provider = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy service provider với id {id}.");

        return ServiceProviderResponse.From(provider);
    }

    public async Task<ServiceProviderResponse> CreateAsync(CreateServiceProviderRequest request)
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

        var provider = new ServiceProviderEntity
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

        return ServiceProviderResponse.From(provider);
    }

    public async Task<ServiceProviderResponse> UpdateAsync(long id, UpdateServiceProviderRequest request)
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

        return ServiceProviderResponse.From(provider);
    }

    public async Task DeleteAsync(long id)
    {
        var provider = await _repository.GetByIdAsync(id);
        if (provider == null || provider.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy service provider với id {id}.");

        provider.DeletedAt = DateTime.UtcNow;
        _repository.Update(provider);
        await _unitOfWork.CommitAsync();
    }
}
