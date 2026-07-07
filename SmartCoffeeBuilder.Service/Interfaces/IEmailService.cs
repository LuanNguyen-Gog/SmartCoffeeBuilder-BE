namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IEmailService
{
    /// <summary>Gửi email HTML thô.</summary>
    Task SendAsync(string toEmail, string subject, string htmlBody);

    /// <summary>
    /// Gửi email từ template trong thư mục EmailTemplates.
    /// <paramref name="templateName"/> là tên file không có đuôi .html (vd: "OtpEmail").
    /// Placeholder dạng {{Key}} trong template sẽ được thay bằng giá trị tương ứng.
    /// </summary>
    Task SendTemplateAsync(string toEmail, string subject, string templateName,
        IReadOnlyDictionary<string, string> placeholders);
}
