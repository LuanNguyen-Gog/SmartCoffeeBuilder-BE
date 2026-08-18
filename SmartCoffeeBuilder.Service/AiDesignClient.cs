using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;

namespace SmartCoffeeBuilder.Service;

public interface IAiDesignClient
{
    Task<AiDesignJobStatusResponse> CreateDesignJobAsync(CreateDesignJobPayload payload, string userId);
    Task<AiDesignJobStatusResponse> GetDesignJobAsync(string jobId, string userId);
    Task<AiDesignJobStatusResponse> RetryDesignJobAsync(string jobId, string userId);
}

public class CreateDesignJobPayload
{
    public string ProjectId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public CafeProjectInput Input { get; set; } = null!;
    public GenerateOptions Options { get; set; } = null!;
    public Guid? RecommendationId { get; set; }
}

public class CafeProjectInput
{
    public string ShopName { get; set; } = null!;
    public string Location { get; set; } = null!;
    public double AreaSqm { get; set; }
    public int FloorCount { get; set; }
    public double? FrontageWidthM { get; set; }
    public decimal BudgetVnd { get; set; }
    public List<string> BusinessGoals { get; set; } = new();
    public List<string> TargetCustomers { get; set; } = new();
    public string BusinessModel { get; set; } = null!;
    public StylePreferencesInput StylePreferences { get; set; } = null!;
    public BrandMoodInput BrandMood { get; set; } = null!;
    public List<string> ColorPreferences { get; set; } = new();
    public List<string> MustHaveZones { get; set; } = new();
    public List<string> NiceToHaveZones { get; set; } = new();
    public int SeatTarget { get; set; }
    public SiteConstraintsInput SiteConstraints { get; set; } = null!;
    public List<string> ReferenceImageUrls { get; set; } = new();
    public string? Notes { get; set; }
}

public class StylePreferencesInput
{
    public string PrimaryStyle { get; set; } = null!;
    public List<string> SecondaryStyles { get; set; } = new();
    public List<string> InspirationKeywords { get; set; } = new();
}

public class BrandMoodInput
{
    public List<string> Keywords { get; set; } = new();
    public string? Tone { get; set; }
}

public class SiteConstraintsInput
{
    public bool HasExistingStructure { get; set; }
    public string? StructuralNotes { get; set; }
    public bool HasUtilityConstraints { get; set; }
    public string? UtilityNotes { get; set; }
    public int? DeliveryWindowDays { get; set; }
    public bool HasVentilationConstraints { get; set; }
}

public class GenerateOptions
{
    public bool GenerateImage { get; set; }
    public string ImageView { get; set; } = "isometric";
    public string DetailLevel { get; set; } = "medium";
    public int AlternativesCount { get; set; } = 1;
    public string Locale { get; set; } = "vi-VN";
}

public class AiDesignClient : IAiDesignClient
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceToken;
    private readonly string _serviceTokenHeader;
    private readonly ILogger<AiDesignClient> _logger;

    public AiDesignClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AiDesignClient> logger)
    {
        _httpClient = httpClient;
        _serviceToken = configuration["AiDesign:ServiceToken"] ?? "";
        _serviceTokenHeader = configuration["AiDesign:ServiceTokenHeader"] ?? "x-internal-service-token";
        _logger = logger;

        var baseUrl = configuration["AiDesign:BaseUrl"] ?? "http://localhost:8000";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(
            int.TryParse(configuration["AiDesign:TimeoutMs"], out var t) ? t : 30000);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private HttpRequestMessage BuildRequest(string userId)
    {
        var request = new HttpRequestMessage();
        // FastAPI requires X-User-Id header min 3 characters
        var prefixedUserId = userId.StartsWith("user-") ? userId : $"user-{userId}";
        request.Headers.Add("X-User-Id", prefixedUserId);
        if (!string.IsNullOrEmpty(_serviceToken))
        {
            request.Headers.Add(_serviceTokenHeader, _serviceToken);
        }
        return request;
    }

    public async Task<AiDesignJobStatusResponse> CreateDesignJobAsync(CreateDesignJobPayload payload, string userId)
    {
        var request = BuildRequest(userId);
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri(_httpClient.BaseAddress!, "/v1/design-jobs");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        var response = await SendRequestAsync<AiDesignJobStatusResponse>(request);
        return response!;
    }

    public async Task<AiDesignJobStatusResponse> GetDesignJobAsync(string jobId, string userId)
    {
        var request = BuildRequest(userId);
        request.Method = HttpMethod.Get;
        request.RequestUri = new Uri(_httpClient.BaseAddress!, $"/v1/design-jobs/{Uri.EscapeDataString(jobId)}");

        var response = await SendRequestAsync<AiDesignJobStatusResponse>(request);
        return response!;
    }

    public async Task<AiDesignJobStatusResponse> RetryDesignJobAsync(string jobId, string userId)
    {
        var request = BuildRequest(userId);
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri(_httpClient.BaseAddress!, $"/v1/design-jobs/{Uri.EscapeDataString(jobId)}/retry");

        var response = await SendRequestAsync<AiDesignJobStatusResponse>(request);
        return response!;
    }

    private async Task<T?> SendRequestAsync<T>(HttpRequestMessage request) where T : class
    {
        try
        {
            var response = await _httpClient.SendAsync(request);

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("AI raw response: {Json}", json);
                if (typeof(T) == typeof(AiDesignJobStatusResponse))
                {
                    var result = JsonSerializer.Deserialize<AiDesignJobStatusResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    _logger.LogInformation("Deserialized JobId: {JobId}, State: {State}", result?.JobId, result?.State);
                    return result as T;
                }
                return await response.Content.ReadFromJsonAsync<T>();
            }

            await HandleErrorResponseAsync(response);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI service unreachable");
            throw new InvalidOperationException("AI service is unreachable");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "AI service timeout");
            throw new TimeoutException("AI service timed out");
        }
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        string message;
        string rawBody = await response.Content.ReadAsStringAsync();
        
        _logger.LogWarning("AI service error response: Status={Status}, Body={Body}", status, rawBody);

        try
        {
            var errorBody = await response.Content.ReadFromJsonAsync<FastApiErrorBody>();
            message = errorBody?.Error?.Message ?? $"AI service returned {status}";
        }
        catch
        {
            message = $"AI service returned {status}: {rawBody}";
        }

        _logger.LogWarning("AI service error {Status}: {Message}", status, message);

        switch (status)
        {
            case 400:
            case 422:
                throw new ArgumentException(message);
            case 401:
                throw new UnauthorizedAccessException(message);
            case 403:
                throw new UnauthorizedAccessException(message);
            case 404:
                throw new KeyNotFoundException(message);
            case 409:
                throw new InvalidOperationException(message);
            case 504:
                throw new TimeoutException(message);
            default:
                if (status >= 500)
                    throw new Exception($"AI service error: {message}");
                throw new Exception($"AI service returned {status}: {message}");
        }
    }
}

public class FastApiErrorBody
{
    public FastApiErrorDetail? Error { get; set; }
}

public class FastApiErrorDetail
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}
