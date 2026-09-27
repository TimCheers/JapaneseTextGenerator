using KotobaApi.Models;

namespace KotobaApi.Services;

public interface IWordService
{
    Task<List<WordDto>> GetAllForDeckAsync(Guid deckId, Guid currentUserId);
    Task<WordDto?> GetByIdAsync(Guid deckId, Guid id, Guid currentUserId);
    Task<WordDto?> CreateAsync(Guid deckId, CreateWordDto dto, Guid currentUserId);
    Task<bool> UpdateAsync(Guid deckId, Guid id, UpdateWordDto dto, Guid currentUserId);
    Task<bool> DeleteAsync(Guid deckId, Guid id, Guid currentUserId);
    Task<List<WordDto>?> ImportFromExcelAsync(Guid deckId, Stream fileStream, Guid currentUserId);
    Task<List<Word>> GetAllForUserAsync(Guid userId);
}