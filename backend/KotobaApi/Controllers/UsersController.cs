using KotobaApi.Authorization;
using KotobaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    public UsersController(IUserService service) => _service = service;

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterUserDto dto)
    {
        var authResponse = await _service.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = authResponse.User.Id }, authResponse);
    }

    [HttpGet("{id}")]
    [SameUser("id")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _service.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }
}