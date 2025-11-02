using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerceApp.Application.Interfaces; // IJwtTokenService
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerceApp.Infrastructure.Services
{
    /// <summary>
    /// JWT minting service compatible with IJwtTokenService.Create(string, string, int).
    ///  - arg1: subject (username/userId)
    ///  - arg2: audience (or role name; we store it as audience and also as "roleName" claim)
    ///  - arg3: roleId (1=user, 2=admin)
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string? _issuer;
        private readonly string? _audienceDefault;

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
            var audience = string.IsNullOrWhiteSpace(audienceOrRoleName) ? _audienceDefault : audienceOrRoleName;
            var extra = new Dictionary<string, string>
            {
                { "roleName", string.IsNullOrWhiteSpace(audienceOrRoleName) ? (roleId == 2 ? "Admin" : "User") : audienceOrRoleName }
            };

            return CreateToken(subject, roleId, audience, extra, TimeSpan.FromHours(24));
        }

        // Convenience
        public string Create(string subject, int roleId)
            => CreateToken(subject, roleId, _audienceDefault, null, TimeSpan.FromHours(24));

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
                new(ClaimTypes.Role, roleId == 2 ? "Admin" : "User"),
                new("roleId", roleId.ToString())
            };

            if (extraClaims != null)
                foreach (var kv in extraClaims)
                    claims.Add(new Claim(kv.Key, kv.Value));

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(24));

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
