namespace SmartCoffeeBuilder.Service.DTOs.Requests;

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public decimal? AreaM2 { get; set; }
    public decimal? Budget { get; set; }

    /// <summary>briefed | in_progress | completed | cancelled</summary>
    public string? Status { get; set; }
}
