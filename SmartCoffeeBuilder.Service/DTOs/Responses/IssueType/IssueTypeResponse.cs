namespace SmartCoffeeBuilder.Service.DTOs.Responses.IssueType;

public class IssueTypeResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public static IssueTypeResponse From(SmartCoffeeBuilder.Repository.Models.IssueType e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name
    };
}
