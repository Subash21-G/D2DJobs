using JobForFresher.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace JobForFresher.Services;
public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var host = configuration["EmailSettings:SmtpServer"];
        var sender = configuration["EmailSettings:SenderEmail"];
        var password = configuration["EmailSettings:SenderPassword"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email delivery is not configured.");
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(configuration["EmailSettings:SenderName"] ?? "D2DJobs", sender));
        email.To.Add(MailboxAddress.Parse(toEmail)); email.Subject = subject;
        email.Body = new TextPart("html") { Text = message };
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, configuration.GetValue("EmailSettings:SmtpPort", 587), SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(sender, password);
        await smtp.SendAsync(email); await smtp.DisconnectAsync(true);
    }
}