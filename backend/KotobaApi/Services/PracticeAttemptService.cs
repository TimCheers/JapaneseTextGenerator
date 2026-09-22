using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class PracticeAttemptService : IPracticeAttemptService
{
    private readonly AppDbContext _db;
    public PracticeAttemptService(AppDbContext db) => _db = db;

    public async Task<PracticeAttemptDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var attempt = await _db.PracticeAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        return attempt is null ? null : ToDto(attempt);
    }

    public async Task<PracticeAttemptDto> CreateAsync(Guid userId, Guid generatedTextId)
    {
        var attempt = new PracticeAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GeneratedTextId = generatedTextId,
            StartedAt = DateTimeOffset.UtcNow
        };

        _db.PracticeAttempts.Add(attempt);
        await _db.SaveChangesAsync();

        return ToDto(attempt);
    }
    
    public async Task<PracticeAttemptDto?> CompleteAsync(Guid id, Guid userId)
    {
        var attempt = await _db.PracticeAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (attempt is null) return null;

        attempt.Score = attempt.Answers.Count(a => a.IsCorrect);
        attempt.CompletedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return ToDto(attempt);
    }

    private static PracticeAttemptDto ToDto(PracticeAttempt a) =>
        new(a.Id, a.GeneratedTextId, a.StartedAt, a.CompletedAt, a.Score);
}
