using SmartCoffeeBuilder.Service.Utils;
using Entity = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Quotation;

/// <summary>
/// Báo giá kèm hạng mục + điều kiện thanh toán + file đính kèm — đây là thứ owner đọc để SO SÁNH
/// các provider (thay cho một dòng Apply.Proposal trước đây).
/// </summary>
public class QuotationResponse
{
    public Guid Id { get; set; }
    public Guid? ApplyId { get; set; }
    public Guid? ProjectWorkingId { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = null!;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public int? FreeRevisionCount { get; set; }

    /// <summary>Phí mỗi vòng sửa vượt hạn mức miễn phí. null = chưa công bố đơn giá.</summary>
    public decimal? ExtraRevisionFee { get; set; }

    /// <summary>draft | sent | revision_requested | accepted | rejected | superseded</summary>
    public string Status { get; set; } = null!;

    public string? RevisionReason { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? LockedAt { get; set; }

    /// <summary>Đã khoá thì provider không sửa được nữa — FE dùng để ẩn nút sửa.</summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Thông tin provider gửi báo giá, kèm sẵn để owner so sánh các bản cạnh nhau mà không phải
    /// gọi thêm API hồ sơ từng người (review 3: "hiển thị chi tiết profile của provider").
    /// </summary>
    public string? ProviderName { get; set; }

    public Guid? ServiceProviderProfileId { get; set; }
    public decimal? ProviderAvgRating { get; set; }
    public int? ProviderYearsExperience { get; set; }
    public bool? ProviderIsVerified { get; set; }

    public List<QuotationItemResponse> Items { get; set; } = new();
    public List<QuotationPaymentTermResponse> PaymentTerms { get; set; } = new();
    public List<QuotationAttachmentResponse> Attachments { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static QuotationResponse From(Entity.Quotation e) => new()
    {
        Id = e.Id,
        ApplyId = e.ApplyId,
        ProjectWorkingId = e.ProjectWorkingId,
        Version = e.Version,
        Title = e.Title,
        Note = e.Note,
        TotalAmount = e.TotalAmount,
        EstimatedDurationDays = e.EstimatedDurationDays,
        FreeRevisionCount = e.FreeRevisionCount,
        ExtraRevisionFee = e.ExtraRevisionFee,
        Status = e.Status.ToString(),
        RevisionReason = e.RevisionReason,
        RejectReason = e.RejectReason,
        SentAt = e.SentAt,
        RespondedAt = e.RespondedAt,
        LockedAt = e.LockedAt,
        IsLocked = e.LockedAt != null,
        ServiceProviderProfileId = e.Apply?.ServiceProviderProfileId ?? e.ProjectWorking?.ServiceProviderProfileId,
        ProviderName = ProviderOf(e)?.DisplayName,
        ProviderAvgRating = ProviderOf(e)?.AvgRating,
        ProviderYearsExperience = ProviderOf(e)?.YearsExperience,
        ProviderIsVerified = ProviderOf(e)?.IsVerified,
        Items = e.Items.OrderBy(i => i.SortOrder).Select(QuotationItemResponse.From).ToList(),
        PaymentTerms = e.PaymentTerms.OrderBy(t => t.SortOrder).Select(QuotationPaymentTermResponse.From).ToList(),
        Attachments = e.Attachments.OrderBy(a => a.CreatedAt).Select(QuotationAttachmentResponse.From).ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };

    /// <summary>
    /// Hồ sơ provider treo ở hai chỗ khác nhau tuỳ đường vào: qua hồ sơ ứng tuyển (Apply) hay
    /// qua lời mời trực tiếp (ProjectWorking). Trả null khi caller không Include nhánh tương ứng.
    /// </summary>
    private static Entity.ServiceProviderProfile? ProviderOf(Entity.Quotation e) =>
        e.Apply?.ServiceProviderProfile ?? e.ProjectWorking?.ServiceProviderProfile;
}

public class QuotationItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }

    public static QuotationItemResponse From(Entity.QuotationItem e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Unit = e.Unit,
        Quantity = e.Quantity,
        UnitPrice = e.UnitPrice,
        Amount = e.Amount,
        Note = e.Note,
        SortOrder = e.SortOrder
    };
}

public class QuotationPaymentTermResponse
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; } = null!;
    public decimal? Percentage { get; set; }
    public decimal Amount { get; set; }
    public string? Condition { get; set; }

    public static QuotationPaymentTermResponse From(Entity.QuotationPaymentTerm e) => new()
    {
        Id = e.Id,
        SortOrder = e.SortOrder,
        Name = e.Name,
        Percentage = e.Percentage,
        Amount = e.Amount,
        Condition = e.Condition
    };
}

public class QuotationAttachmentResponse
{
    public Guid Id { get; set; }
    /// <summary>ObjectName lưu trong DB.</summary>
    public string FileUrl { get; set; } = null!;
    /// <summary>URL public tuyệt đối — FE dùng thẳng để xem/tải.</summary>
    public string? FileViewUrl { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }

    public static QuotationAttachmentResponse From(Entity.QuotationAttachment e) => new()
    {
        Id = e.Id,
        FileUrl = e.FileUrl,
        FileViewUrl = MediaUrl.Resolve(e.FileUrl),
        FileName = e.FileName,
        CreatedAt = e.CreatedAt
    };
}
