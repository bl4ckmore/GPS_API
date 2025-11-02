using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ECommerceApp.Infrastructure.Email;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<MailKitEmailSender> _log;

    public MailKitEmailSender(IOptions<EmailOptions> opt, ILogger<MailKitEmailSender> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromAddress));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var sso = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_opt.Host, _opt.Port, sso, ct);

            if (!string.IsNullOrWhiteSpace(_opt.User))
                await client.AuthenticateAsync(_opt.User, _opt.Password, ct);

            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);

            _log.LogInformation("Email sent to {To}. Subject={Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Email send failed to {To}. Subject={Subject}", toEmail, subject);
            throw;
        }
    }
}
