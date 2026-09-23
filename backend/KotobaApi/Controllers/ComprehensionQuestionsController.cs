using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/generated-texts/{generatedTextId}/questions")]
public class ComprehensionQuestionsController : ControllerBase
{
    private readonly IComprehensionQuestionService _service;
    public ComprehensionQuestionsController(IComprehensionQuestionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<ComprehensionQuestionDto>>> GetAll(Guid userId, Guid generatedTextId) =>
        Ok(await _service.GetByGeneratedTextIdAsync(generatedTextId, userId));
}
