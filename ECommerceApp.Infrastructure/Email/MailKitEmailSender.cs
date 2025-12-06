using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Infrastructure.Data; // Ensure this points to where your DbContext is
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerceApp.Infrastructure.Email
{
    public sealed class MailKitEmailSender : IEmailSender
    {
        private readonly EmailOptions _opt;
        private readonly ILogger<MailKitEmailSender> _log;
        private readonly IServiceProvider _serviceProvider; // We need this to get DbContext cleanly

        public MailKitEmailSender(
            IOptions<EmailOptions> opt,
            ILogger<MailKitEmailSender> log,
            IServiceProvider serviceProvider)
        {
            _opt = opt.Value;
            _log = log;
            _serviceProvider = serviceProvider;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var msg = new MimeMessage();

            // 1. Prepare the Email
            msg.From.Add(new MailboxAddress(_opt.FromName.Trim(), _opt.FromEmail.Trim())); // Use FromEmail, not FromAddress matches your JSON
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            // 2. Create the Log Entry (Pending Status)
            var emailLog = new EmailLog
            {
                To = toEmail,
                Subject = subject,
                BodyContent = htmlBody.Length > 4000 ? htmlBody.Substring(0, 4000) : htmlBody, // Truncate if too long
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                using var client = new SmtpClient();

                // Brevo specific: Port 587 requires StartTls
                var sso = _opt.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_opt.Host.Trim(), _opt.Port, sso, ct);

                if (!string.IsNullOrWhiteSpace(_opt.User))
                {
                    await client.AuthenticateAsync(_opt.User.Trim(), _opt.Password.Trim(), ct);
                }

                await client.SendAsync(msg, ct);
                await client.DisconnectAsync(true, ct);

                // Success! Update Log
                emailLog.Status = "Success";
                emailLog.SentAt = DateTime.UtcNow;
                _log.LogInformation("Email sent successfully to {To}", toEmail);
            }
            catch (Exception ex)
            {
                // Failure! Update Log
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;
                _log.LogError(ex, "Failed to send email to {To}", toEmail);

                // We do NOT throw here if we want the user flow to continue, 
                // but usually, for password resets, you might want to know it failed.
                // For now, we swallow the error so the app doesn't crash, but we log it.
            }
            finally
            {
                // 3. Save Log to Database
                // We create a new Scope because EmailSender might be Singleton/Transient 
                // but DbContext is Scoped.
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Change ApplicationDbContext to your actual Context Name

                context.EmailLogs.Add(emailLog);
                await context.SaveChangesAsync(ct);
            }
        }
    }
}