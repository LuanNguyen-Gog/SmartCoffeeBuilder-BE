using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.DailyLog;

// Folder "DailyLog" biến DailyLog thành namespace con (CS0118) và che mất entity cùng tên —
// cùng lý do đã ghi ở CommentResponse. Phải alias tường minh.
using DailyLogEntity = SmartCoffeeBuilder.Repository.Models.DailyLog;
using DailyLogMediaEntity = SmartCoffeeBuilder.Repository.Models.DailyLogMedia;
using AccountEntity = SmartCoffeeBuilder.Repository.Models.Account;
// Property MediaUrl của DTO che mất static class Utils.MediaUrl (CS0120) — alias để gọi được resolver.
using MediaUrlResolver = SmartCoffeeBuilder.Service.Utils.MediaUrl;

public class DailyLogMediaResponse
{
    public Guid Id { get; set; }

    /// <summary>Giá trị lưu trong DB (ObjectName trên bucket).</summary>
    public string MediaUrl { get; set; } = null!;

    /// <summary>URL public tuyệt đối — FE dùng thẳng cho img src / player.</summary>
    public string? MediaViewUrl { get; set; }

    /// <summary>image | video</summary>
    public string MediaType { get; set; } = null!;

    public string? Caption { get; set; }
    public int SortOrder { get; set; }

    public static DailyLogMediaResponse From(DailyLogMediaEntity m) => new()
    {
        Id = m.Id,
        MediaUrl = m.MediaUrl,
        MediaViewUrl = MediaUrlResolver.Resolve(m.MediaUrl),
        MediaType = m.MediaType.ToString(),
        Caption = m.Caption,
        SortOrder = m.SortOrder
    };
}

public class DailyLogResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ConstructionItemId { get; set; }

    /// <summary>Tên hạng mục — để FE khỏi gọi thêm một vòng chỉ để hiện nhãn.</summary>
    public string? ConstructionItemName { get; set; }

    public Guid? ConstructionTaskId { get; set; }
    public string? ConstructionTaskName { get; set; }

    public DateOnly LogDate { get; set; }
    public string WorkDone { get; set; } = null!;
    public string? IssueNote { get; set; }
    public string? WeatherNote { get; set; }
    public int? WorkerCount { get; set; }

    public Guid? CreatedBy { get; set; }

    /// <summary>Tên hiển thị người ghi nhật ký.</summary>
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<DailyLogMediaResponse> Media { get; set; } = [];

    public static DailyLogResponse From(DailyLogEntity e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ConstructionItemId = e.ConstructionItemId,
        ConstructionItemName = e.ConstructionItem?.Name,
        ConstructionTaskId = e.ConstructionTaskId,
        ConstructionTaskName = e.ConstructionTask?.Name,
        LogDate = e.LogDate,
        WorkDone = e.WorkDone,
        IssueNote = e.IssueNote,
        WeatherNote = e.WeatherNote,
        WorkerCount = e.WorkerCount,
        CreatedBy = e.CreatedBy,
        CreatedByName = ResolveDisplayName(e.CreatedByAccount),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Media = [.. e.Media.OrderBy(m => m.SortOrder).Select(DailyLogMediaResponse.From)]
    };

    private static string? ResolveDisplayName(AccountEntity? account)
    {
        if (account == null) return null;
        if (account.ServiceProviderProfile != null) return account.ServiceProviderProfile.DisplayName;
        if (account.ShopOwner != null) return account.ShopOwner.FullName;
        return account.Email;
    }
}
