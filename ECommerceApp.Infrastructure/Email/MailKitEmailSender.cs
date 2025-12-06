using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Core.Entities;
using ECommerceApp.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection; // Needed for IServiceScopeFactory

namespace ECommerceApp.Infrastructure.Email;

public sealed class MailKitEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<MailKitEmailSender> _log;
    // We use ScopeFactory because EmailSender is often Singleton/Transient, 
    // but DbContext is Scoped. This prevents "DbContext Disposed" errors.
    private readonly IServiceScopeFactory _scopeFactory;

    public MailKitEmailSender(
        IOptions<EmailOptions> opt,
        ILogger<MailKitEmailSender> log,
        IServiceScopeFactory scopeFactory)
    {
        _opt = opt.Value;
        _log = log;
        _scopeFactory = scopeFactory;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();

        // Use Trim() to ensure no hidden spaces cause errors
        msg.From.Add(new MailboxAddress(_opt.FromName.Trim(), _opt.FromEmail.Trim()));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        // Prepare Log Entry
        var emailLog = new EmailLog
        {
            To = toEmail,
            Subject = subject,
            BodyContent = htmlBody.Length > 4000 ? htmlBody.Substring(0, 4000) : htmlBody,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        using var client = new SmtpClient();
        try
        {
            // Connect
            // Port 465 = SSL, Port 587 = StartTLS
            var sso = _opt.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_opt.Host.Trim(), _opt.Port, sso, ct);

            if (!string.IsNullOrWhiteSpace(_opt.User))
            {
                await client.AuthenticateAsync(_opt.User.Trim(), _opt.Password.Trim(), ct);
            }

            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);

            // Success
            emailLog.Status = "Success";
            emailLog.SentAt = DateTime.UtcNow;
            _log.LogInformation("Email sent successfully to {To}", toEmail);
        }
        catch (Exception ex)
        {
            // Failure
            emailLog.Status = "Failed";
            emailLog.ErrorMessage = ex.Message;
            _log.LogError(ex, "Failed to send email to {To}", toEmail);
            // We swallow the error so the Order process doesn't crash
        }
        finally
        {
            // Save Log to DB using a fresh scope
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.EmailLogs.Add(emailLog);
                await context.SaveChangesAsync(ct);
            }
            catch (Exception dbEx)
            {
                _log.LogError(dbEx, "Failed to save EmailLog to database");
            }
        }
    }
}