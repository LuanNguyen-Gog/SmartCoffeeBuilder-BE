namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Lookup table.</summary>
public class IssueType
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
