using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/decks/{deckId}/words")]
public class WordsController : ControllerBase
{
    private readonly IWordService _service;
    public WordsController(IWordService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<WordDto>>> GetAll(Guid deckId) =>
        Ok(await _service.GetAllForDeckAsync(deckId));

    [HttpGet("{id}")]
    public async Task<ActionResult<WordDto>> GetById(Guid deckId, Guid id)
    {
        var word = await _service.GetByIdAsync(deckId, id);
        return word is null ? NotFound() : Ok(word);
    }

    [HttpPost]
    public async Task<ActionResult<WordDto>> Create(Guid deckId, CreateWordDto dto)
    {
        var word = await _service.CreateAsync(deckId, dto);
        return CreatedAtAction(nameof(GetById), new { deckId, id = word.Id }, word);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid deckId, Guid id, UpdateWordDto dto) =>
        await _service.UpdateAsync(deckId, id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid deckId, Guid id) =>
        await _service.DeleteAsync(deckId, id) ? NoContent() : NotFound();
    
    [HttpPost("import")]
    public async Task<ActionResult<List<WordDto>>> Import(Guid deckId, IFormFile file)
    {
        var words = await _service.ImportFromExcelAsync(deckId, file.OpenReadStream());
        return Ok(words);
    }
}
