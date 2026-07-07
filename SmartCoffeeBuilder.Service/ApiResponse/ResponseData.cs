namespace SmartCoffeeBuilder.Service.ApiResponse;

/// <summary>
/// Envelope chuẩn cho mọi response của API: bọc data kèm trạng thái thành công /
/// thông điệp / mã HTTP / timestamp. Service trả về kiểu này, Controller trả thẳng cho client.
/// </summary>
public class ResponseData<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; } = ApiStatusCodes.Status200OK;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ResponseData() { }

    public ResponseData(bool success, T? data, string? message, int statusCode)
    {
        Success = success;
        Data = data;
        Message = message;
        StatusCode = statusCode;
    }

    public static ResponseData<T> Ok(T? data, string? message = null)
        => new(true, data, message, ApiStatusCodes.Status200OK);

    public static ResponseData<T> Created(T? data, string? message = null)
        => new(true, data, message, ApiStatusCodes.Status201Created);

    public static ResponseData<T> Fail(string message, int statusCode = ApiStatusCodes.Status400BadRequest)
        => new(false, default, message, statusCode);
}

/// <summary>Hằng số mã HTTP để Service không phải reference Microsoft.AspNetCore.Http.</summary>
internal static class ApiStatusCodes
{
    public const int Status200OK = 200;
    public const int Status201Created = 201;
    public const int Status400BadRequest = 400;
}
