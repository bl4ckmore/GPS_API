// ECommerceApp.Infrastructure/Email/MailKitEmailSender.cs

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ECommerceApp.Core.Interfaces;

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

            // 🎯 FIX 1: Change _opt.Username to _opt.User to match EmailOptions.cs
            if (!string.IsNullOrWhiteSpace(_opt.User))
                await client.AuthenticateAsync(_opt.User, _opt.Password, ct);

            await client.SendAsync(msg, ct);

            _log.LogInformation("Email sent successfully to {To}. Subject={Subject}", toEmail, subject);
        }
        catch (MailKit.Security.AuthenticationException authEx)
        {
            _log.LogError(authEx, "Email send failed due to authentication error. Check App Password and Username for {Host}.", _opt.Host);
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Email send failed to {To} due to connection/server error.", toEmail);
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, ct);
        }
    }
}