using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/practice-attempts")]
public class PracticeAttemptsController : ControllerBase
{
    private readonly IPracticeAttemptService _service;
    public PracticeAttemptsController(IPracticeAttemptService service) => _service = service;

    [HttpGet("{id}")]
    public async Task<ActionResult<PracticeAttemptDto>> GetById(Guid userId, Guid id)
    {
        var result = await _service.GetByIdAsync(id, userId);
        return result is null ? NotFound() : Ok(result);
    }

    // POST api/users/{userId}/practice-attempts?generatedTextId=...
    [HttpPost]
    public async Task<ActionResult<PracticeAttemptDto>> Create(Guid userId, [FromQuery] Guid generatedTextId)
    {
        var result = await _service.CreateAsync(userId, generatedTextId);
        return CreatedAtAction(nameof(GetById), new { userId, id = result.Id }, result);
    }

    // PUT api/users/{userId}/practice-attempts/{id}/complete
    [HttpPut("{id}/complete")]
    public async Task<ActionResult<PracticeAttemptDto>> Complete(Guid userId, Guid id)
    {
        var result = await _service.CompleteAsync(id, userId);
        return result is null ? NotFound() : Ok(result);
    }
}
