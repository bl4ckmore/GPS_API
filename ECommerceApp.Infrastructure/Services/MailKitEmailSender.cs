using System.Threading;
using System.Threading.Tasks;
using ECommerceApp.Application.Interfaces; // IEmailSender
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ECommerceApp.Infrastructure.Services
{
    /// <summary>
    /// MailKit-based email sender implementing IEmailSender.SendAsync(string to, string subject, string body, CancellationToken ct)
    /// </summary>
    public sealed class MailKitEmailSender : IEmailSender
    {
        public sealed class Options
        {
            public string From { get; set; } = "no-reply@gpshub.local";
            public string Host { get; set; } = "smtp.gmail.com";
            public int Port { get; set; } = 587;
            public string? User { get; set; } = null;
            public string? Pass { get; set; } = null;
            public bool UseStartTls { get; set; } = true; // STARTTLS on 587
        }

        private readonly Options _opt;

        public MailKitEmailSender(Options opt) => _opt = opt;

        /// <summary>
        /// Sends an email. If the body looks like HTML (contains a tag), we send HTML; otherwise plain text.
        /// Matches IEmailSender interface: SendAsync(string to, string subject, string body, CancellationToken ct)
        /// </summary>
        public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(_opt.From));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject ?? string.Empty;

            // naive HTML detection; adjust if you prefer forced text
            var looksLikeHtml = !string.IsNullOrWhiteSpace(body) && body.IndexOf('<') >= 0 && body.IndexOf('>') > 0;

            msg.Body = looksLikeHtml
                ? new BodyBuilder { HtmlBody = body }.ToMessageBody()
                : new TextPart("plain") { Text = body ?? string.Empty };

            using var client = new SmtpClient();

            // Connect
            await client.ConnectAsync(
                _opt.Host,
                _opt.Port,
                _opt.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                ct
            );

            // Auth if provided
            if (!string.IsNullOrWhiteSpace(_opt.User))
            {
                await client.AuthenticateAsync(_opt.User, _opt.Pass ?? string.Empty, ct);
            }

            // Send
            await client.SendAsync(msg, ct);

            // Disconnect
            await client.DisconnectAsync(true, ct);
        }
    }
}
