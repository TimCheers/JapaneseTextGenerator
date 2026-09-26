using KotobaApi.Models;

namespace KotobaApi.Services;

public interface IWordService
{
    Task<List<WordDto>> GetAllForDeckAsync(Guid deckId);
    Task<WordDto?> GetByIdAsync(Guid deckId, Guid id);
    Task<WordDto> CreateAsync(Guid deckId, CreateWordDto dto);
    Task<bool> UpdateAsync(Guid deckId, Guid id, UpdateWordDto dto);
    Task<bool> DeleteAsync(Guid deckId, Guid id);
    Task<List<WordDto>> ImportFromExcelAsync(Guid deckId, Stream fileStream);
    Task<List<Word>> GetAllForUserAsync(Guid userId);
}