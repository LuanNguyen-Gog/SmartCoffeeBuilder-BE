namespace SmartCoffeeBuilder.Service.DTOs.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public long UserId { get; set; }
    public string Email { get; set; } = null!;
    public IEnumerable<string> Roles { get; set; } = [];
}
