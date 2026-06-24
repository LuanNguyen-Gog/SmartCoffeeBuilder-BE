namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Lookup table — phân loại tài liệu kỹ thuật.</summary>
public class DocType
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<Doc> Docs { get; set; } = new List<Doc>();
}
