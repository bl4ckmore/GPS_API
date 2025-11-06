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
        // 🎯 FIX 1: Declare msg and client outside the try block
        var msg = new MimeMessage();
        using var client = new SmtpClient(); // client must also be outside the try block

        // Apply Trim() to FromName and FromAddress when constructing the message
        msg.From.Add(new MailboxAddress(_opt.FromName.Trim(), _opt.FromAddress.Trim()));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            // Trim Host, User, and Password values for safety (Now accessing local vars)
            var host = _opt.Host.Trim();
            var user = _opt.User.Trim();
            var password = _opt.Password.Trim();

            // Determine SSO based on port configuration
            SecureSocketOptions sso;
            if (_opt.Port == 465)
                sso = SecureSocketOptions.SslOnConnect; // Use implicit TLS/SSL for port 465
            else
                sso = SecureSocketOptions.StartTls;    // Use explicit TLS for 587, 2525

            await client.ConnectAsync(host, _opt.Port, sso, ct);

            if (!string.IsNullOrWhiteSpace(user))
                await client.AuthenticateAsync(user, password, ct);

            // This line caused the error when msg was inside the try block:
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
            // This is where the original error was, because client was not found.
            if (client.IsConnected)
                await client.DisconnectAsync(true, ct);
        }
    }
}