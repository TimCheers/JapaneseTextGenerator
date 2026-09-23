namespace KotobaApi.Services;

public interface IGeneratedTextService
{
    Task<GeneratedTextDto?> GetByIdAsync(Guid id, Guid userId);
    Task<GeneratedTextDto?> GetByGenerationRequestIdAsync(Guid generationRequestId, Guid userId);
}
