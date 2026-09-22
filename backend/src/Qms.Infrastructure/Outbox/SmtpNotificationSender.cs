using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Qms.Infrastructure.Outbox;

public sealed class SmtpNotificationSender(IConfiguration configuration)
{
    public async Task SendAsync(string recipient, string subject, string body,
        CancellationToken cancellationToken)
    {
        var host = configuration["Notifications:Smtp:Host"];
        var sender = configuration["Notifications:Smtp:Sender"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("SMTP bildirim kanalı yapılandırılmamıştır.");
        using var message = new MailMessage(sender, recipient, subject, body) { IsBodyHtml = false };
        using var client = new SmtpClient(host, configuration.GetValue("Notifications:Smtp:Port", 587))
        {
            EnableSsl = configuration.GetValue("Notifications:Smtp:UseTls", true)
        };
        var username = configuration["Notifications:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, configuration["Notifications:Smtp:Password"]);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
