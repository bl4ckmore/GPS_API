using System.IdentityModel.Tokens.Jwt;             
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;              
using System.Text;
using ECommerceApp.Application.Interfaces;         

namespace ECommerceApp.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _cfg;
        public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

        public string Create(string subject, string roleName, int roleId)
        {
            var sec = _cfg.GetSection("Jwt");
            var key = sec["Key"]!;
            var issuer = sec["Issuer"];
            var audience = sec["Audience"];
            var minutes = int.TryParse(sec["AccessTokenMinutes"], out var m) ? m : 120;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(ClaimTypes.NameIdentifier, subject),
                new(ClaimTypes.Name, subject),
                new(ClaimTypes.Role, roleName),
                new("roleId", roleId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
