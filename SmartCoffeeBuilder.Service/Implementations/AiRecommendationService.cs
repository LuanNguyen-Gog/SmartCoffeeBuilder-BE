using System.Text.Json;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Messaging;

namespace SmartCoffeeBuilder.Service.Implementations;

public class AiRecommendationService : IAiRecommendationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<AiRecommendation> _repository;
    private readonly IMessageBusService _messageBus;

    public AiRecommendationService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IMessageBusService messageBus)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<AiRecommendation>();
        _messageBus = messageBus;
    }

    public async Task<PaginationResponse<AiRecommendationResponse>> GetAllByBriefIdAsync(
        long briefId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _repository
            .GetQueryable(r => r.BriefId == briefId)
            .OrderByDescending(r => r.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<AiRecommendationResponse>(
            paged.Items.Select(AiRecommendationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<AiRecommendationResponse> GetByIdAsync(long id)
    {
        var recommendation = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request)
    {
        _ = await _unitOfWork.GetRepository<DesignBrief>()
            .SingleOrDefaultAsync(predicate: b => b.Id == request.BriefId)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {request.BriefId}.");

        var recommendation = new AiRecommendation
        {
            BriefId = request.BriefId,
            ConceptSummary = request.ConceptSummary,
            Payload = request.Payload,
            EstimatedDesignCost = request.EstimatedDesignCost,
            EstimatedConstructionCost = request.EstimatedConstructionCost,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(recommendation);
        await _unitOfWork.CommitAsync();

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task<AiRecommendationResponse> UpdateAsync(long id, UpdateAiRecommendationRequest request)
    {
        var recommendation = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        if (request.ConceptSummary != null) recommendation.ConceptSummary = request.ConceptSummary;
        if (request.Payload != null) recommendation.Payload = request.Payload;
        if (request.EstimatedDesignCost.HasValue) recommendation.EstimatedDesignCost = request.EstimatedDesignCost;
        if (request.EstimatedConstructionCost.HasValue) recommendation.EstimatedConstructionCost = request.EstimatedConstructionCost;

        // AI Design Job updates
        if (request.JobId != null) recommendation.JobId = request.JobId;
        if (request.State != null) recommendation.State = request.State;
        if (request.LastError != null) recommendation.LastError = request.LastError;
        if (request.Attempts.HasValue) recommendation.Attempts = request.Attempts.Value;
        if (request.StartedAt.HasValue) recommendation.StartedAt = request.StartedAt;
        if (request.CompletedAt.HasValue) recommendation.CompletedAt = request.CompletedAt;
        if (request.ParentJobId != null) recommendation.ParentJobId = request.ParentJobId;

        // Plan fields
        if (request.PlanConceptName != null) recommendation.PlanConceptName = request.PlanConceptName;
        if (request.PlanSummary != null) recommendation.PlanSummary = request.PlanSummary;
        if (request.LayoutWidth.HasValue) recommendation.LayoutWidth = request.LayoutWidth;
        if (request.LayoutHeight.HasValue) recommendation.LayoutHeight = request.LayoutHeight;
        if (request.LayoutUnit != null) recommendation.LayoutUnit = request.LayoutUnit;
        if (request.LayoutZones != null) recommendation.LayoutZones = request.LayoutZones;
        if (request.LayoutAdjacencyRules != null) recommendation.LayoutAdjacencyRules = request.LayoutAdjacencyRules;
        if (request.FitoutMinVnd.HasValue) recommendation.FitoutMinVnd = request.FitoutMinVnd;
        if (request.FitoutMaxVnd.HasValue) recommendation.FitoutMaxVnd = request.FitoutMaxVnd;
        if (request.EquipmentMinVnd.HasValue) recommendation.EquipmentMinVnd = request.EquipmentMinVnd;
        if (request.EquipmentMaxVnd.HasValue) recommendation.EquipmentMaxVnd = request.EquipmentMaxVnd;
        if (request.ContingencyPercent.HasValue) recommendation.ContingencyPercent = request.ContingencyPercent;
        if (request.CostNotes != null) recommendation.CostNotes = request.CostNotes;
        if (request.CustomerFlow != null) recommendation.CustomerFlow = request.CustomerFlow;
        if (request.Recommendations != null) recommendation.Recommendations = request.Recommendations;
        if (request.RiskNotes != null) recommendation.RiskNotes = request.RiskNotes;
        if (request.ImageView != null) recommendation.ImageView = request.ImageView;
        if (request.ImagePrompt != null) recommendation.ImagePrompt = request.ImagePrompt;
        if (request.ImageAspectRatio != null) recommendation.ImageAspectRatio = request.ImageAspectRatio;
        if (request.ImageNegativePrompt != null) recommendation.ImageNegativePrompt = request.ImageNegativePrompt;
        if (request.ImageReferenceUrls != null) recommendation.ImageReferenceUrls = request.ImageReferenceUrls;
        if (request.ImageArtifactUrl != null) recommendation.ImageArtifactUrl = request.ImageArtifactUrl;
        if (request.SeatCapacityRecommendation.HasValue) recommendation.SeatCapacityRecommendation = request.SeatCapacityRecommendation;
        if (request.PlanJson != null) recommendation.PlanJson = request.PlanJson;

        _repository.Update(recommendation);
        await _unitOfWork.CommitAsync();

        return AiRecommendationResponse.From(recommendation);
    }

    public async Task DeleteAsync(long id)
    {
        var recommendation = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy ai recommendation với id {id}.");

        _repository.Delete(recommendation);
        await _unitOfWork.CommitAsync();
    }

    // ========== AI Design Job Methods ==========

    public async Task<AiDesignJobStatusResponse> GenerateDesignAsync(long briefId, string userId, GenerateAiDesignRequest request)
    {
        // Phí nền tảng: shop owner gói free KHÔNG dùng được AI — phải có subscription active.
        await EnsureAiAccessAsync(userId);

        // Load DesignBrief với Project
        var brief = await _unitOfWork.GetRepository<DesignBrief>()
            .SingleOrDefaultAsync(predicate: b => b.Id == briefId)
            ?? throw new KeyNotFoundException($"Không tìm thấy design brief với id {briefId}.");

        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == brief.ProjectShopOwnerId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project liên quan.");

        // Required fields for AI worker — fail fast if project metadata is incomplete
        // so we don't enqueue jobs that worker will reject at validation.
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(project.Name)) missingFields.Add(nameof(project.Name));
        if (string.IsNullOrWhiteSpace(project.Address)) missingFields.Add(nameof(project.Address));
        if (project.AreaM2 <= 0) missingFields.Add(nameof(project.AreaM2));
        if (project.Budget <= 0) missingFields.Add(nameof(project.Budget));
        if (missingFields.Count > 0)
        {
            throw new InvalidOperationException(
                $"Project {project.Id} thiếu các trường bắt buộc cho AI design: {string.Join(", ", missingFields)}. " +
                "Vui lòng cập nhật project trước khi generate AI design.");
        }

        // Generate JobId upfront as GUID for consistent tracking
        var jobId = Guid.NewGuid().ToString();

        // Create AIRecommendation record first
        var recommendation = new AiRecommendation
        {
            BriefId = briefId,
            JobId = jobId,
            State = "queued",
            CreatedAt = DateTime.UtcNow,
            ConceptSummary = $"AI Design: {project.Name}",
            Payload = JsonSerializer.Serialize(new { request, projectId = project.Id, briefId })
        };

        await _repository.InsertAsync(recommendation);
        await _unitOfWork.CommitAsync();

        // Parse JSON fields từ DesignBrief
        var businessGoals = ParseJsonList(brief.BusinessGoals) ?? new List<string> { "cafe_operation" };
        var targetCustomers = ParseJsonList(brief.TargetCustomer) ?? new List<string> { "general" };
        var brandMoodKeywords = ParseJsonList(brief.Mood) ?? new List<string> { "cozy", "relaxing" };

        // Publish message to RabbitMQ for AI service to process
        var message = new AiDesignRequestMessage
        {
            RecommendationId = recommendation.Id,
            BriefId = briefId,
            UserId = userId,
            ProjectId = $"proj-{project.Id}",
            RequestedAt = DateTime.UtcNow,
            Payload = new AiDesignRequestPayload
            {
                ShopName = project.Name,
                Location = project.Address,
                AreaSqm = (double)project.AreaM2,
                FloorCount = 1,
                BudgetVnd = project.Budget,
                BusinessGoals = businessGoals,
                TargetCustomers = targetCustomers,
                BusinessModel = MapBusinessModel(brief.BusinessModel),
                PrimaryStyle = brief.Style ?? "modern",
                BrandMoodKeywords = brandMoodKeywords,
                MustHaveZones = request.MustHaveZones ?? new List<string> { "counter", "seating_area", "restroom" },
                NiceToHaveZones = request.NiceToHaveZones ?? new List<string> { "outdoor_seating", "private_room" },
                SeatTarget = brief.SeatCount ?? 20,
                ReferenceImageUrls = request.ReferenceImageUrls ?? new List<string>(),
                Notes = request.Notes ?? brief.BrandNote,
                GenerateImage = request.GenerateImage,
                ImageView = request.ImageView?.ToString().ToLowerInvariant(),
                DetailLevel = request.DetailLevel?.ToString().ToLowerInvariant()
            }
        };

        await _messageBus.PublishAsync(QueueKeys.AiDesignRequest, message);

        return new AiDesignJobStatusResponse
        {
            Id = recommendation.Id,
            JobId = recommendation.JobId,
            ProjectId = $"proj-{project.Id}",
            UserId = userId,
            State = "queued",
            CreatedAt = recommendation.CreatedAt,
            Attempts = 0
        };
    }

    public async Task ProcessAiDesignResultAsync(AiDesignResultMessage result)
    {
        var recommendation = await _repository.GetByIdAsync(result.RecommendationId)
            ?? throw new KeyNotFoundException($"Không tìm thấy recommendation với id {result.RecommendationId}.");

        recommendation.JobId = result.JobId;
        recommendation.State = result.State;
        recommendation.LastError = result.Error;
        recommendation.CompletedAt = result.CompletedAt;

        // Map plan fields
        recommendation.PlanConceptName = result.ConceptName;
        recommendation.PlanSummary = result.Summary;
        recommendation.LayoutWidth = result.LayoutWidth;
        recommendation.LayoutHeight = result.LayoutHeight;
        recommendation.LayoutUnit = result.LayoutUnit;
        recommendation.LayoutZones = result.Zones;
        recommendation.LayoutAdjacencyRules = result.AdjacencyRules;
        recommendation.FitoutMinVnd = result.FitoutMinVnd;
        recommendation.FitoutMaxVnd = result.FitoutMaxVnd;
        recommendation.EquipmentMinVnd = result.EquipmentMinVnd;
        recommendation.EquipmentMaxVnd = result.EquipmentMaxVnd;
        recommendation.ContingencyPercent = result.ContingencyPercent;
        recommendation.CostNotes = result.CostNotes;
        recommendation.CustomerFlow = result.CustomerFlow;
        recommendation.Recommendations = result.Recommendations;
        recommendation.RiskNotes = result.RiskNotes;
        recommendation.ImageView = result.ImageView;
        recommendation.ImagePrompt = result.ImagePrompt;
        recommendation.ImageAspectRatio = result.ImageAspectRatio;
        recommendation.ImageNegativePrompt = result.ImageNegativePrompt;
        recommendation.ImageReferenceUrls = result.ImageReferenceUrls;
        recommendation.ImageArtifactUrl = result.ImageArtifactUrl;
        recommendation.SeatCapacityRecommendation = result.SeatCapacityRecommendation;

        // Store raw JSON for debugging
        recommendation.PlanJson = JsonSerializer.Serialize(result);

        _repository.Update(recommendation);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Guard phí nền tảng cho tính năng AI: account role owner phải có subscription active còn hạn.
    /// Admin/provider không bị chặn ở đây (owner là đối tượng bán gói AI).
    /// </summary>
    private async Task EnsureAiAccessAsync(string userId)
    {
        // TODO(rào tạm): Đang mở khoá AI cho MỌI account để FE test luồng/UI ổn định.
        // Bỏ dòng return dưới đây để bật lại gate phí nền tảng (owner phải có subscription active).
        return;
#pragma warning disable CS0162 // Unreachable code detected
        if (!long.TryParse(userId, out var accountId))
            throw new UnauthorizedAccessException("User ID trong token không hợp lệ.");

        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với id {accountId}.");

        if (account.Role != AccountRole.owner) return;

        var now = DateTime.UtcNow;
        var hasActiveSubscription = await _unitOfWork.GetRepository<Subscription>().CountAsync(
            s => s.AccountId == accountId
                 && s.Status == SubscriptionStatus.active
                 && s.EndDate > now) > 0;

        if (!hasActiveSubscription)
            throw new InvalidOperationException(
                "Tài khoản gói free không dùng được tính năng AI design. " +
                "Vui lòng mua gói subscription (GET /api/payments/plans) để mở khoá.");
#pragma warning restore CS0162
    }

    private static List<string>? ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return new List<string> { json };
        }
    }

    private static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string MapBusinessModel(string? model) => model?.ToLowerInvariant() switch
    {
        "chain_franchise" => "chain_franchise",
        "specialty_cafe" => "specialty_cafe",
        "quick_service" => "quick_service",
        "hybrid_retail" => "hybrid_retail",
        _ => "independent"
    };
}
