namespace KotobaApi.Services;

public interface IUserAnswerService
{
    Task<List<UserAnswerDto>> GetByAttemptIdAsync(Guid practiceAttemptId, Guid userId);
    Task<UserAnswerDto?> SubmitAsync(Guid practiceAttemptId, Guid userId, SubmitAnswerDto dto);
}
