using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ShopOwnerService : IShopOwnerService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ShopOwner> _repository;

    public ShopOwnerService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ShopOwner>();
    }

    public async Task<PaginationResponse<ShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        var paged = await _repository
            .GetQueryable()
            .OrderByDescending(s => s.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ShopOwnerResponse>(
            paged.Items.Select(ShopOwnerResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ShopOwnerResponse> GetByIdAsync(Guid id)
    {
        var shopOwner = await _repository.SingleOrDefaultAsync(predicate: s => s.Id == id)
            ?? throw new KeyNotFoundException($"No shop owner found with id {id}.");

        return ShopOwnerResponse.From(shopOwner);
    }

    public async Task<ShopOwnerResponse> CreateAsync(CreateShopOwnerRequest request)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == request.AccountId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"No account found with id {request.AccountId}.");

        if (account.Role != AccountRole.owner)
            throw new ArgumentException("The account must have role 'owner' to create a shop owner profile.");

        var existing = await _repository.SingleOrDefaultAsync(predicate: s => s.AccountId == request.AccountId);
        if (existing != null)
            throw new InvalidOperationException("This account already has a shop owner profile.");

        var shopOwner = new ShopOwner
        {
            AccountId = request.AccountId,
            FullName = request.FullName,
            ShopName = request.ShopName,
            Phone = request.Phone,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(shopOwner);
        await _unitOfWork.CommitAsync();

        return ShopOwnerResponse.From(shopOwner);
    }

    public async Task<ShopOwnerResponse> UpdateAsync(Guid id, UpdateShopOwnerRequest request)
    {
        var shopOwner = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No shop owner found with id {id}.");

        if (request.FullName != null) shopOwner.FullName = request.FullName;
        if (request.ShopName != null) shopOwner.ShopName = request.ShopName;
        if (request.Phone != null) shopOwner.Phone = request.Phone;
        if (request.Address != null) shopOwner.Address = request.Address;

        shopOwner.UpdatedAt = DateTime.UtcNow;
        _repository.Update(shopOwner);
        await _unitOfWork.CommitAsync();

        return ShopOwnerResponse.From(shopOwner);
    }

    public async Task DeleteAsync(Guid id)
    {
        var shopOwner = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"No shop owner found with id {id}.");

        _repository.Delete(shopOwner);
        await _unitOfWork.CommitAsync();
    }
}
