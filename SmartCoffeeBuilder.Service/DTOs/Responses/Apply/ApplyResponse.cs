using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Apply;

/// <summary>
/// Hồ sơ ứng tuyển như owner nhìn thấy.
///
/// Review 3 chốt: một dòng <see cref="Proposal"/> là không đủ để chọn giữa nhiều provider. Nên
/// response mang thêm (a) tóm tắt hồ sơ năng lực + điểm đánh giá TÁCH THEO HẠNG MỤC, và (b) tình
/// trạng báo giá kèm theo — chi tiết báo giá đọc qua <c>GET /api/quotations?applyId=</c>.
/// </summary>
public class ApplyResponse
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string? PostTitle { get; set; }
    public Guid? ProjectShopOwnerId { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string? ProviderDisplayName { get; set; }

    /// <summary>Thư ngỏ ngắn của provider. Bảng giá chi tiết nằm ở quotation, không nhét vào đây.</summary>
    public string Proposal { get; set; } = null!;

    public int? EstimatedDurationDays { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Hồ sơ năng lực provider (review 3: "hiển thị chi tiết profile của provider, portfolio…") ──

    /// <summary>designer | constructor | both</summary>
    public string? ProviderCapability { get; set; }

    /// <summary>individual | company</summary>
    public string? ProviderType { get; set; }

    public string? ProviderBio { get; set; }
    public string? ProviderPortfolioHeadline { get; set; }
    public int? ProviderYearsExperience { get; set; }
    public bool? ProviderIsVerified { get; set; }
    public decimal? ProviderAvgRating { get; set; }

    /// <summary>Số dự án provider đã hoàn thành trên hệ thống (engagement 'completed').</summary>
    public int? ProviderCompletedProjects { get; set; }

    /// <summary>
    /// Điểm trung bình TÁCH THEO HẠNG MỤC (vd: "Tiến độ thi công": 4.5) — lấy từ review_scores.
    /// Đúng yêu cầu review 3: feedback phải xem được theo từng hạng mục, không chỉ một điểm tổng.
    /// </summary>
    public Dictionary<string, decimal>? ProviderRatingByDimension { get; set; }

    // ── Báo giá kèm hồ sơ (review 3) ──

    /// <summary>Số bản báo giá provider đã gửi cho hồ sơ này.</summary>
    public int QuotationCount { get; set; }

    /// <summary>Tổng tiền của bản báo giá đang có hiệu lực (mới nhất chưa bị thay thế).</summary>
    public decimal? LatestQuotationAmount { get; set; }

    /// <summary>Trạng thái bản báo giá mới nhất — null khi provider chưa gửi báo giá nào.</summary>
    public string? LatestQuotationStatus { get; set; }

    public Guid? LatestQuotationId { get; set; }

    // ── Khảo sát kèm hồ sơ ──
    //
    // Owner so nhiều provider rồi mới chọn, nên phải thấy AI đã đi khảo sát thực tế chứ không
    // chỉ ai đã báo giá. Với bài đăng có pha thiết kế, đây cũng là điều kiện để accept được
    // (xem ApplyService.EnsureSurveySubmittedAsync) — trả ra đây để owner biết TRƯỚC khi bấm.

    /// <summary>Số bản khảo sát provider đã nộp cho hồ sơ này.</summary>
    public int SurveyCount { get; set; }

    /// <summary>Bản khảo sát mới nhất — null khi provider chưa nộp bản nào.</summary>
    public Guid? LatestSurveyId { get; set; }

    /// <summary>Lịch hẹn khảo sát của bản mới nhất.</summary>
    public DateTime? LatestSurveyScheduledAt { get; set; }

    /// <summary>
    /// Thời điểm provider ĐÃ đi khảo sát thực tế. Null nghĩa là mới chỉ hẹn lịch — với bài đăng
    /// có pha thiết kế thì owner chưa accept được hồ sơ này.
    /// </summary>
    public DateTime? LatestSurveyedAt { get; set; }

    /// <summary>Đã đi khảo sát thực tế chưa — rút gọn của <see cref="LatestSurveyedAt"/> != null.</summary>
    public bool HasCompletedSurvey { get; set; }

    public static ApplyResponse From(SmartCoffeeBuilder.Repository.Models.Apply a)
    {
        var provider = a.ServiceProviderProfile;

        // Bản báo giá "đang nói chuyện" là bản version cao nhất — các bản cũ đã superseded/rejected.
        var latestQuotation = a.Quotations?
            .OrderByDescending(q => q.Version)
            .FirstOrDefault();

        // "Mới nhất" theo thời điểm tạo — survey không có version như quotation.
        var latestSurvey = a.Surveys?
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        return new ApplyResponse
        {
            Id = a.Id,
            PostId = a.PostId,
            PostTitle = a.Post?.Title,
            ProjectShopOwnerId = a.Post?.ProjectShopOwnerId,
            ServiceProviderProfileId = a.ServiceProviderProfileId,
            ProviderDisplayName = provider?.DisplayName,
            Proposal = a.Proposal,
            EstimatedDurationDays = a.EstimatedDurationDays,
            Status = a.Status.ToString(),
            SubmittedAt = a.SubmittedAt,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,

            ProviderCapability = provider?.Capability.ToString(),
            ProviderType = provider?.ProviderType.ToString(),
            ProviderBio = provider?.Bio,
            ProviderPortfolioHeadline = provider?.PortfolioHeadline,
            ProviderYearsExperience = provider?.YearsExperience,
            ProviderIsVerified = provider?.IsVerified,
            ProviderAvgRating = provider?.AvgRating,

            // Các trường dưới đây chỉ có khi caller nạp kèm engagement + review (xem ApplyService).
            ProviderCompletedProjects = provider?.ProjectWorkings
                ?.Count(e => e.Status == ProviderStatus.completed),
            ProviderRatingByDimension = BuildRatingByDimension(provider),

            QuotationCount = a.Quotations?.Count ?? 0,
            LatestQuotationId = latestQuotation?.Id,
            LatestQuotationAmount = latestQuotation?.TotalAmount,
            LatestQuotationStatus = latestQuotation?.Status.ToString(),

            SurveyCount = a.Surveys?.Count ?? 0,
            LatestSurveyId = latestSurvey?.Id,
            LatestSurveyScheduledAt = latestSurvey?.ScheduledAt,
            LatestSurveyedAt = latestSurvey?.SurveyedAt,
            HasCompletedSurvey = a.Surveys?.Any(s => s.SurveyedAt != null) ?? false
        };
    }

    /// <summary>
    /// Gộp điểm theo tên hạng mục đánh giá trên mọi engagement đã hoàn thành của provider.
    /// Trả null (không phải dictionary rỗng) khi caller không nạp review — để FE phân biệt được
    /// "chưa có dữ liệu đánh giá" với "không yêu cầu phần này".
    /// </summary>
    private static Dictionary<string, decimal>? BuildRatingByDimension(
        SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile? provider)
    {
        var scores = provider?.ProjectWorkings?
            .SelectMany(e => e.Reviews)
            .SelectMany(r => r.ReviewScores)
            .ToList();

        if (scores is null || scores.Count == 0) return null;

        return scores
            .GroupBy(s => s.Dimension)
            .ToDictionary(
                g => g.Key.ToString(),
                g => decimal.Round(g.Average(s => (decimal)s.Score), 1));
    }
}
