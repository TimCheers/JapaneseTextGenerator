using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KotobaApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace KotobaApi.Services;

public interface ITokenService
{
    string CreateToken(User user);
}
