namespace KotobaApi.Services;

public interface IGenerationRequestService
{
    Task<GenerationRequestDto?> GetByIdAsync(Guid id, Guid userId);
    Task<List<GenerationRequestDto>> GetAllForUserAsync(Guid userId);
    Task<GenerationRequestDto> CreateAsync(Guid userId, CreateGenerationRequestDto dto);
}
