using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _service;
    public AuthController(IUserService service) => _service = service;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var authResponse = await _service.LoginAsync(dto);
        return authResponse is null ? Unauthorized() : Ok(authResponse);
    }
}