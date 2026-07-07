using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Gửi email qua Gmail API (REST) bằng OAuth2 refresh token — không dùng SMTP.
/// Cấu hình ở section Email: ClientId/ClientSecret của OAuth client và RefreshToken
/// (lấy qua OAuth consent với scope gmail.send). UserCredential tự đổi refresh token
/// lấy access token và tự làm mới khi hết hạn.
/// Template HTML đặt trong thư mục EmailTemplates, placeholder dạng {{Key}}.
/// </summary>
public class EmailService : IEmailService
{
    private const string ApplicationName = "Smart Coffee Builder";

    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    private static readonly string TemplateDirectory =
        Path.Combine(AppContext.BaseDirectory, "EmailTemplates");

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var senderEmail = _configuration["Email:SenderEmail"]
            ?? throw new InvalidOperationException("Missing configuration: Email:SenderEmail");
        var senderName = _configuration["Email:SenderName"] ?? ApplicationName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var gmail = CreateGmailService();

        // Gmail API nhận toàn bộ MIME đã mã hoá base64url ở trường Raw.
        var gmailMessage = new Message { Raw = EncodeToBase64Url(message) };
        // "me" = tài khoản chủ của access token (SenderEmail).
        await gmail.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();

        _logger.LogInformation("Đã gửi email '{Subject}' tới {ToEmail}", subject, toEmail);
    }

    public async Task SendTemplateAsync(string toEmail, string subject, string templateName,
        IReadOnlyDictionary<string, string> placeholders)
    {
        var htmlBody = await RenderTemplateAsync(templateName, placeholders);
        await SendAsync(toEmail, subject, htmlBody);
    }

    // ──────────────────────────────────────────────────────────────
    private GmailService CreateGmailService()
    {
        var clientId = _configuration["Email:ClientId"]
            ?? throw new InvalidOperationException("Missing configuration: Email:ClientId");
        var clientSecret = _configuration["Email:ClientSecret"]
            ?? throw new InvalidOperationException("Missing configuration: Email:ClientSecret");
        var refreshToken = _configuration["Email:RefreshToken"]
            ?? throw new InvalidOperationException("Missing configuration: Email:RefreshToken");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = [GmailService.Scope.GmailSend]
        });

        // UserCredential dùng refresh token để lấy/renew access token khi cần.
        var credential = new UserCredential(flow, "user", new TokenResponse { RefreshToken = refreshToken });

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    private static string EncodeToBase64Url(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        // base64url: thay +/ và bỏ padding = theo yêu cầu của Gmail API.
        return Convert.ToBase64String(stream.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", string.Empty);
    }

    private static async Task<string> RenderTemplateAsync(
        string templateName, IReadOnlyDictionary<string, string> placeholders)
    {
        var path = Path.Combine(TemplateDirectory, $"{templateName}.html");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Không tìm thấy email template '{templateName}.html' trong thư mục EmailTemplates.", path);

        var html = await File.ReadAllTextAsync(path);
        foreach (var (key, value) in placeholders)
            html = html.Replace($"{{{{{key}}}}}", value);

        return html;
    }
}
