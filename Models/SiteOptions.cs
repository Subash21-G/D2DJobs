using System.ComponentModel.DataAnnotations;
namespace JobForFresher.Models;
public class SiteOptions : IValidatableObject
{
    [StringLength(250)] public string SiteUrl { get; set; } = "";
    [StringLength(250)] public string TelegramUrl { get; set; } = "";
    [StringLength(250)] public string WhatsAppUrl { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SiteUrl) && (!Uri.TryCreate(SiteUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.AbsolutePath != "/" || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != ""))
            yield return new("Enter the HTTPS origin only, for example https://jobs.example.com.", [nameof(SiteUrl)]);
        if (!IsChannel(TelegramUrl, "t.me")) yield return new("Use an HTTPS t.me channel URL.", [nameof(TelegramUrl)]);
        if (!IsChannel(WhatsAppUrl, "whatsapp.com") && !IsChannel(WhatsAppUrl, "www.whatsapp.com") && !IsChannel(WhatsAppUrl, "chat.whatsapp.com"))
            yield return new("Use an HTTPS WhatsApp channel or group URL.", [nameof(WhatsAppUrl)]);
    }
    private static bool IsChannel(string value, string host) => string.IsNullOrWhiteSpace(value) || (Uri.TryCreate(value, UriKind.Absolute, out var url) && url.Scheme == "https" && url.Host == host && url.UserInfo == "");
}