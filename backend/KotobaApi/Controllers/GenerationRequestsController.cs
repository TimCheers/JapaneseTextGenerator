using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/generation-requests")]
public class GenerationRequestsController : ControllerBase
{
    private readonly IGenerationRequestService _service;
    public GenerationRequestsController(IGenerationRequestService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<GenerationRequestDto>>> GetAll(Guid userId) =>
        Ok(await _service.GetAllForUserAsync(userId));

    [HttpGet("{id}")]
    public async Task<ActionResult<GenerationRequestDto>> GetById(Guid userId, Guid id)
    {
        var result = await _service.GetByIdAsync(id, userId);
        return result is null ? NotFound() : Ok(result);
    }
    
    [HttpPost]
    public async Task<ActionResult<GenerationRequestDto>> Create(Guid userId, CreateGenerationRequestDto dto)
    {
        var result = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { userId, id = result.Id }, result);
    }
}
