using KotobaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class WordProgressService : IWordProgressService
{
    private readonly AppDbContext _db;
    public WordProgressService(AppDbContext db) => _db = db;
    
    public async Task<List<WordProgressDto>> GetDueForUserAsync(Guid userId, DateTimeOffset asOf) =>
        await _db.WordProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.DueAt <= asOf)
            .Select(p => new WordProgressDto(p.Id, p.WordId, p.Stability, p.StabilityFast, p.Difficulty, p.DueAt, p.LastReviewedAt, p.ReviewCount, p.LapseCount))
            .ToListAsync();
}
