using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/decks")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _service;
    public DecksController(IDeckService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<DeckDto>>> GetAll(Guid userId) =>
        Ok(await _service.GetAllForUserAsync(userId));

    [HttpGet("{id}")]
    public async Task<ActionResult<DeckDto>> GetById(Guid userId, Guid id)
    {
        var deck = await _service.GetByIdAsync(userId, id);
        return deck is null ? NotFound() : Ok(deck);
    }

    [HttpPost]
    public async Task<ActionResult<DeckDto>> Create(Guid userId, CreateDeckDto dto)
    {
        var deck = await _service.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { userId, id = deck.Id }, deck);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid userId, Guid id, UpdateDeckDto dto) =>
        await _service.UpdateAsync(userId, id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id) =>
        await _service.DeleteAsync(userId, id) ? NoContent() : NotFound();
}