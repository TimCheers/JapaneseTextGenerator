using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KotobaApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace KotobaApi.Services;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;
    public JwtTokenService(IConfiguration config) => _config = config;

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
        };
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiresMinutes"]!)),
            signingCredentials: credentials, issuer: _config["Jwt:Issuer"], audience: _config["Jwt:Audience"]);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
