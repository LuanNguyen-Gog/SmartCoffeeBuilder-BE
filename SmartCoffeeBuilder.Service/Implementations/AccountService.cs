using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Account;
using SmartCoffeeBuilder.Service.DTOs.Responses.Account;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Account> _repository;

    public AccountService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Account>();
    }

    public async Task<PaginationResponse<AccountResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        var paged = await _repository
            .GetQueryable(a => a.DeletedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<AccountResponse>(
            paged.Items.Select(AccountResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<AccountResponse> GetByIdAsync(Guid id)
    {
        var account = await _repository.SingleOrDefaultAsync(predicate: a => a.Id == id && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy account với id {id}.");

        return AccountResponse.From(account);
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        var existing = await _repository.SingleOrDefaultAsync(predicate: a => a.Email == request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email đã được sử dụng.");

        if (!Enum.TryParse<AccountRole>(request.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Cho phép: owner, provider, admin.");

        var status = AccountStatus.active;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && !Enum.TryParse(request.Status, ignoreCase: true, out status))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: active, inactive, banned, pending.");

        var account = new Account
        {
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(account);
        await _unitOfWork.CommitAsync();

        return AccountResponse.From(account);
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account == null || account.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy account với id {id}.");

        if (request.Phone != null) account.Phone = request.Phone;

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!Enum.TryParse<AccountRole>(request.Role, ignoreCase: true, out var role))
                throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Cho phép: owner, provider, admin.");
            account.Role = role;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<AccountStatus>(request.Status, ignoreCase: true, out var status))
                throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: active, inactive, banned, pending.");
            account.Status = status;
        }

        account.UpdatedAt = DateTime.UtcNow;
        _repository.Update(account);
        await _unitOfWork.CommitAsync();

        return AccountResponse.From(account);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account == null || account.DeletedAt != null)
            throw new KeyNotFoundException($"Không tìm thấy account với id {id}.");

        var activeEngagements = await _unitOfWork.GetRepository<ProjectWorking>().CountAsync(
            e => (e.Status == ProviderStatus.requested || e.Status == ProviderStatus.accepted)
                 && (e.ServiceProviderProfile.AccountId == id || e.ProjectShopOwner.Owner.AccountId == id));
        if (activeEngagements > 0)
            throw new InvalidOperationException(
                $"Tài khoản còn {activeEngagements} engagement đang hoạt động — đóng/huỷ hết trước khi xoá tài khoản.");

        // Owner còn dự án đang 'in_progress' (đã bắt đầu chạy) thì phải nghiệm thu/huỷ trước.
        var inProgressProjects = await _unitOfWork.GetRepository<ProjectShopOwner>().CountAsync(
            p => p.DeletedAt == null && p.Owner.AccountId == id && p.Status == ProjectStatus.in_progress);
        if (inProgressProjects > 0)
            throw new InvalidOperationException(
                $"Tài khoản còn {inProgressProjects} dự án đang 'in_progress' — nghiệm thu/huỷ trước khi xoá tài khoản.");

        account.DeletedAt = DateTime.UtcNow;
        _repository.Update(account);

        // Thu hồi mọi refresh token còn hiệu lực → khoá phiên đăng nhập ngay
        // (access token cũ vẫn sống tới khi hết hạn ≤ Jwt:AccessTokenExpirationMinutes).
        var tokenRepo = _unitOfWork.GetRepository<RefreshToken>();
        var activeTokens = await tokenRepo.GetListAsync(
            predicate: rt => rt.AccountId == id && rt.RevokedAt == null);
        foreach (var t in activeTokens) t.RevokedAt = DateTime.UtcNow;
        tokenRepo.UpdateRange(activeTokens);

        await _unitOfWork.CommitAsync(); // account + tokens trong một transaction
    }

    public async Task<PaginationResponse<AccountResponse>> SearchAsync(
        int pageNumber = 1, int pageSize = 10,
        string? role = null, string? status = null, string? search = null, bool includeDeleted = false)
    {
        AccountRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<AccountRole>(role, ignoreCase: true, out var parsedRole))
                throw new ArgumentException($"Role '{role}' không hợp lệ. Cho phép: owner, provider, admin.");
            roleFilter = parsedRole;
        }

        AccountStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsedStatus))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: active, inactive, banned, pending.");
            statusFilter = parsedStatus;
        }

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var paged = await _repository
            .GetQueryable(a =>
                (includeDeleted || a.DeletedAt == null)
                && (roleFilter == null || a.Role == roleFilter)
                && (statusFilter == null || a.Status == statusFilter)
                && (term == null || a.Email.Contains(term) || (a.Phone != null && a.Phone.Contains(term))))
            .OrderByDescending(a => a.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<AccountResponse>(
            paged.Items.Select(AccountResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<AccountResponse> SetStatusAsync(Guid id, string status)
    {
        if (!Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsedStatus))
            throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: active, inactive, banned, pending.");

        var account = await _repository.SingleOrDefaultAsync(predicate: a => a.Id == id && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy account với id {id}.");

        account.Status = parsedStatus;
        account.UpdatedAt = DateTime.UtcNow;
        _repository.Update(account);
        await _unitOfWork.CommitAsync();

        return AccountResponse.From(account);
    }
}
