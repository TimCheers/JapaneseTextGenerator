using KotobaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class GeneratedTextService : IGeneratedTextService
{
    private readonly AppDbContext _db;
    public GeneratedTextService(AppDbContext db) => _db = db;
    
    public async Task<GeneratedTextDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var text = await _db.GeneratedTexts
            .AsNoTracking()
            .Include(t => t.GenerationRequest)
            .FirstOrDefaultAsync(t => t.Id == id && t.GenerationRequest.UserId == userId);

        return text is null ? null : new GeneratedTextDto(text.Id, text.GenerationRequestId, text.Content, text.CreatedAt);
    }

    public async Task<GeneratedTextDto?> GetByGenerationRequestIdAsync(Guid generationRequestId, Guid userId)
    {
        var text = await _db.GeneratedTexts
            .AsNoTracking()
            .Include(t => t.GenerationRequest)
            .FirstOrDefaultAsync(t => t.GenerationRequestId == generationRequestId && t.GenerationRequest.UserId == userId);

        return text is null ? null : new GeneratedTextDto(text.Id, text.GenerationRequestId, text.Content, text.CreatedAt);
    }

    public async Task<List<GeneratedTextDto>> GetAllForUserAsync(Guid userId)
    {
        var text = await _db.GeneratedTexts
            .AsNoTracking()
            .Include(t => t.GenerationRequest)
            .Where(t => t.GenerationRequest.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new GeneratedTextDto(t.Id, t.GenerationRequestId, t.Content, t.CreatedAt))
            .ToListAsync();

        return text;
    }
}
