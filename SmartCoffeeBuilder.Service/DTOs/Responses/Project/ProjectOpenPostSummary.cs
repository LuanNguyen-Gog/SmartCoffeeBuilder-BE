using SmartCoffeeBuilder.Repository.Models;
using ProjectPostModel = SmartCoffeeBuilder.Repository.Models.ProjectPost;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Project;

/// <summary>
/// Thông tin rút gọn của một post đang mở, đính kèm trong <see cref="ProjectResponse"/>.
/// </summary>
public class ProjectOpenPostSummary
{
    public long Id { get; set; }
    public string ServiceKind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? SubmissionDeadline { get; set; }

    public static ProjectOpenPostSummary From(ProjectPostModel p) => new()
    {
        Id = p.Id,
        ServiceKind = p.ServiceKind.ToString(),
        Title = p.Title,
        Status = p.Status.ToString(),
        SubmissionDeadline = p.SubmissionDeadline
    };
}