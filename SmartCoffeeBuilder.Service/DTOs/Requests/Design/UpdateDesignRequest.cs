namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class UpdateDesignRequest
{
    public string? Title { get; set; }

    /// <summary>concept | layout_2d | render_3d | technical_drawing</summary>
    public string? Type { get; set; }
}
