using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerceApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerceApp.Infrastructure.Services
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string? _issuer;
        private readonly string? _audienceDefault;

        // ✅ SET TO 24 HOURS (This fixes the "Logout Immediately" bug)
        private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(24);

        public JwtTokenService(IConfiguration cfg)
        {
            var key = cfg["Auth:Jwt:Key"] ?? cfg["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("JWT key missing. Set Auth:Jwt:Key or Jwt:Key.");

            var bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length < 32)
                throw new InvalidOperationException($"JWT key too short: {bytes.Length} bytes.");

            _key = new SymmetricSecurityKey(bytes);
            _issuer = cfg["Auth:Jwt:Issuer"];
            _audienceDefault = cfg["Auth:Jwt:Audience"];
        }

        public string Create(string subject, string audienceOrRoleName, int roleId)
        {
            var audience = string.IsNullOrWhiteSpace(audienceOrRoleName) ? _audienceDefault : audienceOrRoleName;
            var roleName = string.IsNullOrWhiteSpace(audienceOrRoleName) || audienceOrRoleName.Contains(".")
                           ? (roleId == 2 ? "Admin" : "User")
                           : audienceOrRoleName;

            var extra = new Dictionary<string, string> { { "roleName", roleName } };
            return CreateToken(subject, roleId, audience, extra, _tokenLifetime);
        }

        public string Create(string subject, int roleId)
            => CreateToken(subject, roleId, _audienceDefault, null, _tokenLifetime);

        private string CreateToken(
            string subject,
            int roleId,
            string? audienceOverride,
            IDictionary<string, string>? extraClaims,
            TimeSpan? lifetime) // <--- Accepting the lifetime here
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(ClaimTypes.NameIdentifier, subject),
                new(ClaimTypes.Name, subject),
                new(ClaimTypes.Role, roleId == 2 ? "Admin" : "User"),
                new("roleId", roleId.ToString())
            };

            if (extraClaims != null)
            {
                foreach (var kv in extraClaims)
                {
                    if (!claims.Any(c => c.Type == kv.Key)) claims.Add(new Claim(kv.Key, kv.Value));
                }
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            // ✅ FIX: Use the passed lifetime (24h) instead of hardcoded 3h
            var actualLifetime = lifetime ?? _tokenLifetime;
            var expires = DateTime.UtcNow.Add(actualLifetime);

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