using Microsoft.EntityFrameworkCore;

namespace SmartCoffeeBuilder.Service.ApiResponse;

/// <summary>
/// Tiện ích phân trang: nhận <see cref="IQueryable{T}"/> (thường lấy từ
/// <c>IGenericRepository&lt;T&gt;.GetQueryable(...)</c>) và trả về <see cref="PaginationResponse{T}"/>.
/// </summary>
public static class PaginationExtensions
{
    public static async Task<PaginationResponse<T>> ToPaginationResponseAsync<T>(
        this IQueryable<T> source, int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var totalItems = await source.CountAsync();
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResponse<T>(items, totalItems, pageNumber, pageSize);
    }
}
