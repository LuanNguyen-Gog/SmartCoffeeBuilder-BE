using Microsoft.EntityFrameworkCore;

namespace SmartCoffeeBuilder.Repository.Utils;

/// <summary>
/// Tiện ích phân trang cho <see cref="IQueryable{T}"/> — đếm tổng và lấy đúng trang dữ liệu.
/// </summary>
public static class QueryablePagingExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> source, int page = 1, int size = 10)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;

        var total = await source.CountAsync();
        var items = await source
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PaginatedResult<T>(items, total, page, size);
    }
}
