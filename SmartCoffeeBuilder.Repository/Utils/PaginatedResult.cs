namespace SmartCoffeeBuilder.Repository.Utils;

/// <summary>
/// Kết quả phân trang ở tầng Repository (không phụ thuộc tầng Service).
/// Service map sang <c>PaginationResponse&lt;T&gt;</c> trước khi trả về client.
/// </summary>
public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int Size { get; init; }
    public int Total { get; init; }

    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Size);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public PaginatedResult() { }

    public PaginatedResult(IReadOnlyList<T> items, int total, int page, int size)
    {
        Items = items;
        Total = total;
        Page = page;
        Size = size;
    }
}
