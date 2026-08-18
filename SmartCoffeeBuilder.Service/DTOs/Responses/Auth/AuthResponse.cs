namespace SmartCoffeeBuilder.Service.DTOs.Responses.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public Guid AccountId { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
}
