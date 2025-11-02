// D:\V4\GPS_API\ecommerceapp.api\Controllers\HeaderAuthHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ECommerceApp.API.Controllers
{
    // Simple header-based auth: if Authorization: X-User <username> is set,
    // authenticate as that username. Keep only for local/dev tools (Swagger).
    public sealed class HeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        // ✅ Renamed to avoid hiding base 'Scheme'
        public const string SchemeName = "HeaderAuth";

        public HeaderAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Expect header: Authorization: X-User <username>
            if (!Request.Headers.TryGetValue("Authorization", out var auth) || auth.Count == 0)
                return Task.FromResult(AuthenticateResult.NoResult());

            var value = auth.ToString();
            const string prefix = "X-User ";
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.NoResult());

            var username = value[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(username))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, username)
            };

            // Use SchemeName instead of base.Scheme to avoid confusion
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
