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
        // 🎯 FIX: Apply Trim() to FromName and FromAddress when constructing the message
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName.Trim(), _opt.FromAddress.Trim()));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var sso = _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            // Trim Host, User, and Password values for safety (Already in place)
            var host = _opt.Host.Trim();
            var user = _opt.User.Trim();
            var password = _opt.Password.Trim();

            await client.ConnectAsync(host, _opt.Port, sso, ct);

            if (!string.IsNullOrWhiteSpace(user))
                await client.AuthenticateAsync(user, password, ct);

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