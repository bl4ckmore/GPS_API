using System.Security.Claims;
using System.Text.Encodings.Web;
using ECommerceApp.Infrastructure.Data;   // DbContext lives in Infrastructure
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ECommerceApp.API.Auth
{
    public class HeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly TimeProvider _time; // if you need current time

        public const string SchemeName = "WhatsGpsHeader";
        public const string HeaderName = "X-WhatsGPS-UserId";

        public HeaderAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ApplicationDbContext db,
            IConfiguration cfg,
            TimeProvider timeProvider)               // ✅ inject TimeProvider instead of ISystemClock
            : base(options, logger, encoder)         // ✅ modern 3-arg base ctor (no clock)
        {
            _db = db;
            _cfg = cfg;
            _time = timeProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var wid = Request.Headers[HeaderName].FirstOrDefault();

            // No header => anonymous
            if (string.IsNullOrWhiteSpace(wid))
                return Task.FromResult(AuthenticateResult.NoResult());

            // Optional: lookup user if you want (left out)

            // Config-based admins (appsettings.json: Admin:WhatsGpsIds)
            var adminIds = _cfg.GetSection("Admin:WhatsGpsIds").Get<string[]>() ?? Array.Empty<string>();
            var role = adminIds.Contains(wid) ? "admin" : "user";

            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, wid),
                new Claim(ClaimTypes.Name, $"user_{wid}"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers["WWW-Authenticate"] = SchemeName;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
