namespace KotobaApi.Services;

public interface IDeckService
{
    Task<List<DeckDto>> GetAllForUserAsync(Guid userId);
    Task<DeckDto?> GetByIdAsync(Guid userId, Guid id);
    Task<DeckDto> CreateAsync(Guid userId, CreateDeckDto dto);
    Task<bool> UpdateAsync(Guid userId, Guid id, UpdateDeckDto dto);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}