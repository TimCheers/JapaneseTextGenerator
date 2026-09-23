namespace KotobaApi.Services;

public interface IPracticeAttemptService
{
    Task<PracticeAttemptDto?> GetByIdAsync(Guid id, Guid userId);
    Task<PracticeAttemptDto> CreateAsync(Guid userId, Guid generatedTextId);
    Task<PracticeAttemptDto?> CompleteAsync(Guid id, Guid userId);
}
