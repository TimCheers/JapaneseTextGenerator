using KotobaApi.Authorization;
using KotobaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/generated-texts/{generatedTextId}/questions")]
[Authorize]
public class ComprehensionQuestionsController : ControllerBase
{
    private readonly IComprehensionQuestionService _service;
    public ComprehensionQuestionsController(IComprehensionQuestionService service) => _service = service;

    [HttpGet]
    [SameUser("userId")]
    public async Task<ActionResult<List<ComprehensionQuestionDto>>> GetAll(Guid userId, Guid generatedTextId) =>
        Ok(await _service.GetByGeneratedTextIdAsync(generatedTextId, userId));
}
