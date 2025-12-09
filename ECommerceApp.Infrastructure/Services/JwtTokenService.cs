using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerceApp.Application.Interfaces; // IJwtTokenService
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerceApp.Infrastructure.Services
{
    /// <summary>
    /// JWT minting service.
    /// Sets token expiration to 24 hours to allow the Frontend Inactivity Tracker (3 hours) 
    /// to manage the session lifetime without premature server-side expiration.
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string? _issuer;
        private readonly string? _audienceDefault;

        // Token Lifetime set to 24 Hours
        // This ensures the user stays logged in as long as they are active.
        // If they are inactive for 3 hours, the Angular app will log them out.
        private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(24);

        public JwtTokenService(IConfiguration cfg)
        {
            var key = cfg["Auth:Jwt:Key"] ?? cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("JWT key missing. Set Auth:Jwt:Key or Jwt:Key.");

            var bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length < 32)
                throw new InvalidOperationException($"JWT key too short: {bytes.Length} bytes. HS256 requires >= 32 bytes.");

            _key = new SymmetricSecurityKey(bytes);
            _issuer = cfg["Auth:Jwt:Issuer"];
            _audienceDefault = cfg["Auth:Jwt:Audience"];
        }

        // Required signature: (subject, audienceOrRoleName, roleId)
        public string Create(string subject, string audienceOrRoleName, int roleId)
        {
            // Determine audience and role name claims
            var audience = string.IsNullOrWhiteSpace(audienceOrRoleName) ? _audienceDefault : audienceOrRoleName;

            // If audienceOrRoleName is strictly an Audience (like "whatsgps"), we still need a role name for the claim
            var roleName = string.IsNullOrWhiteSpace(audienceOrRoleName) || audienceOrRoleName.Contains(".")
                           ? (roleId == 2 ? "Admin" : "User")
                           : audienceOrRoleName;

            var extra = new Dictionary<string, string>
            {
                { "roleName", roleName }
            };

            return CreateToken(subject, roleId, audience, extra, _tokenLifetime);
        }

        // Convenience overload
        public string Create(string subject, int roleId)
            => CreateToken(subject, roleId, _audienceDefault, null, _tokenLifetime);

        private string CreateToken(
            string subject,
            int roleId,
            string? audienceOverride,
            IDictionary<string, string>? extraClaims,
            TimeSpan? lifetime)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(ClaimTypes.NameIdentifier, subject),
                new(ClaimTypes.Name, subject),
                // Standard Role Claim for [Authorize(Roles="...")]
                new(ClaimTypes.Role, roleId == 2 ? "Admin" : "User"),
                // Custom Claim for Frontend Logic
                new("roleId", roleId.ToString())
            };

            if (extraClaims != null)
            {
                foreach (var kv in extraClaims)
                {
                    // Avoid duplicate keys if they already exist
                    if (!claims.Any(c => c.Type == kv.Key))
                    {
                        claims.Add(new Claim(kv.Key, kv.Value));
                    }
                }
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.Add(lifetime ?? _tokenLifetime);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: audienceOverride,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}