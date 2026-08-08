using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Snapshot nguyên trạng một bản <see cref="Design"/> ở một mốc quan trọng (submit / approve) — dùng để
/// truy nguyên lịch sử sau revision. Khi status chuyển submitted → submitted (resubmit sau revision),
/// bản <c>submitted</c> cũ bị ghi đè (xem <see cref="DesignVersionSnapshotKind"/>); bản <c>approved</c> được
/// giữ lại nếu design được approve lại sau revision (lưu thành version mới).
/// </summary>
public class DesignVersion
{
    public long Id { get; set; }

    /// <summary>FK -> <c>designs.id</c>, cascade theo design.</summary>
    public long DesignId { get; set; }

    /// <summary>Loại snapshot (submitted | approved).</summary>
    public DesignVersionSnapshotKind SnapshotKind { get; set; }

    /// <summary>Version decimal(4,1) của design tại thời điểm snapshot (0.1, 0.2…).</summary>
    public decimal Version { get; set; }

    public string? Title { get; set; }

    public DesignType Type { get; set; }

    /// <summary>Status của design tại thời điểm snapshot (thường là submitted hoặc approved).</summary>
    public DesignStatus Status { get; set; }

    /// <summary>Lý do revision tại thời điểm snapshot (nếu có).</summary>
    public string? Reason { get; set; }

    /// <summary>Account id người tạo design gốc (copy nguyên trạng).</summary>
    public long? CreatedBy { get; set; }

    /// <summary>Account id người trigger snapshot (provider nộp / owner duyệt).</summary>
    public long? SnapshottedBy { get; set; }

    /// <summary>CreatedAt của design gốc — giữ lại cho lịch sử.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Thời điểm chụp snapshot.</summary>
    public DateTime SnapshottedAt { get; set; }

    public Design Design { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
    public Account? SnapshottedByAccount { get; set; }
    public ICollection<DesignVersionImage> Images { get; set; } = new List<DesignVersionImage>();
}