using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/word-progress")]
public class WordProgressController : ControllerBase
{
    private readonly IWordProgressService _service;
    public WordProgressController(IWordProgressService service) => _service = service;
    
    [HttpGet("due")]
    public async Task<ActionResult<List<WordProgressDto>>> GetDue(Guid userId) =>
        Ok(await _service.GetDueForUserAsync(userId, DateTimeOffset.UtcNow));
}
