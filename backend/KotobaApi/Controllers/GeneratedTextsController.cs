using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/generated-texts")]
public class GeneratedTextsController : ControllerBase
{
    private readonly IGeneratedTextService _service;
    public GeneratedTextsController(IGeneratedTextService service) => _service = service;

    [HttpGet("{id}")]
    public async Task<ActionResult<GeneratedTextDto>> GetById(Guid userId, Guid id)
    {
        var result = await _service.GetByIdAsync(id, userId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-request/{generationRequestId}")]
    public async Task<ActionResult<GeneratedTextDto>> GetByRequest(Guid userId, Guid generationRequestId)
    {
        var result = await _service.GetByGenerationRequestIdAsync(generationRequestId, userId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<GeneratedTextDto>>> GetAll(Guid userId) =>
        Ok(await _service.GetAllForUserAsync(userId));
}