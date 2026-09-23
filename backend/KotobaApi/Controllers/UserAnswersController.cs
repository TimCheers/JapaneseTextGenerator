using KotobaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Controllers;

[ApiController]
[Route("api/users/{userId}/practice-attempts/{practiceAttemptId}/answers")]
public class UserAnswersController : ControllerBase
{
    private readonly IUserAnswerService _service;
    public UserAnswersController(IUserAnswerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<UserAnswerDto>>> GetAll(Guid userId, Guid practiceAttemptId) =>
        Ok(await _service.GetByAttemptIdAsync(practiceAttemptId, userId));

    [HttpPost]
    public async Task<ActionResult<UserAnswerDto>> Submit(Guid userId, Guid practiceAttemptId, SubmitAnswerDto dto)
    {
        var result = await _service.SubmitAsync(practiceAttemptId, userId, dto);
        return result is null ? NotFound() : Ok(result);
    }
}
