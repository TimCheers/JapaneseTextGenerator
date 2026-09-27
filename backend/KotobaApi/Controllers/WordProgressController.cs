using KotobaApi.Authorization;
using KotobaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/word-progress")]
[Authorize]
public class WordProgressController : ControllerBase
{
    private readonly IWordProgressService _service;
    public WordProgressController(IWordProgressService service) => _service = service;
    
    [HttpGet("due")]
    [SameUser("userId")]
    public async Task<ActionResult<List<WordProgressDto>>> GetDue(Guid userId) =>
        Ok(await _service.GetDueForUserAsync(userId, DateTimeOffset.UtcNow));
}
