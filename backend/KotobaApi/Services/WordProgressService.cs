using KotobaApi.Data;
using KotobaApi.Models;
using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Learning;
using KotobaApi.Srs.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class WordProgressService : IWordProgressService
{
    private readonly AppDbContext _db;
    private readonly IFsrsScheduler _scheduler;

    public WordProgressService(AppDbContext db, IFsrsScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public async Task<List<WordProgressDto>> GetDueForUserAsync(Guid userId, DateTimeOffset asOf) =>
        await _db.WordProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.DueAt <= asOf)
            .Select(p => new WordProgressDto(p.Id, p.WordId, p.Word.Term, p.Word.Reading, p.Word.Meaning,
                p.Stability, p.StabilityFast, p.Difficulty, p.DueAt, p.LastReviewedAt, p.ReviewCount, p.LapseCount))
            .ToListAsync();

    public async Task<WordProgressDto> ApplyReviewAsync(Guid userId, Guid wordId, Guid eventId, FsrsRating rating,
        ReviewSource source, DateTimeOffset reviewedAt)
    {
        Word word = await _db.Words.FirstOrDefaultAsync(w => w.Id == wordId)
            ?? throw new InvalidOperationException($"Word {wordId} does not exist.");
        WordProgress? wordProgress =
            await _db.WordProgresses.FirstOrDefaultAsync(wp => wp.UserId == userId && wp.WordId == wordId);
        WordReviewEvent? newEvent = await _db.WordReviewEvents.FirstOrDefaultAsync(e =>
            e.EventId == eventId && e.WordId == wordId && e.UserId == userId);

        if (newEvent != null && wordProgress != null)
        {
            return new WordProgressDto(wordProgress.Id, wordId, word.Term, word.Reading, word.Meaning,
                newEvent.StabilityAfter, newEvent.StabilityFastAfter, newEvent.DifficultyAfter, newEvent.DueAtAfter,
                newEvent.ReviewedAt, wordProgress.ReviewCount, wordProgress.LapseCount);
        }

        FsrsScheduleRequest scheduleRequest;
        if (wordProgress != null)
        {
            Fsrs7MemoryState previousState = new Fsrs7MemoryState((float)wordProgress.Stability,
                (float)wordProgress.StabilityFast, (float)wordProgress.Difficulty);
            scheduleRequest = new FsrsScheduleRequest(previousState,
                wordProgress.LastReviewedAt, reviewedAt, rating, null);
        }
        else
        {
            scheduleRequest = new FsrsScheduleRequest(null, null, reviewedAt, rating);
        }

        FsrsScheduleResult result = _scheduler.Schedule(scheduleRequest);
        double? oldStability = wordProgress?.Stability;
        double? oldStabilityFast = wordProgress?.StabilityFast;
        double? oldDifficulty = wordProgress?.Difficulty;
        WordProgress newWordProgress;

        if (wordProgress == null)
        {
            newWordProgress = new WordProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WordId = wordId,
                Stability = result.State.Stability,
                StabilityFast = result.State.FastStability,
                Difficulty = result.State.Difficulty,
                DueAt = result.DueAt,
                LastReviewedAt = reviewedAt,
                ReviewCount = 1,
                LapseCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.WordProgresses.Add(newWordProgress);
        }
        else
        {
            newWordProgress = wordProgress;
            newWordProgress.Stability = result.State.Stability;
            newWordProgress.StabilityFast = result.State.FastStability;
            newWordProgress.Difficulty = result.State.Difficulty;
            newWordProgress.DueAt = result.DueAt;
            newWordProgress.LastReviewedAt = reviewedAt;
            newWordProgress.ReviewCount += 1;
            newWordProgress.UpdatedAt = DateTimeOffset.UtcNow;
            if (rating == FsrsRating.Again && !result.WasFirstReview) newWordProgress.LapseCount += 1;
        }

        var reviewEvent = new WordReviewEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            WordId = wordId,
            Rating = rating,
            Source = source,
            ReviewedAt = reviewedAt,
            ElapsedSeconds = (int)(result.ElapsedDays * 86400),
            DesiredRetention = result.DesiredRetention,
            SchedulerVersion = _scheduler.AlgorithmId,
            StabilityBefore = oldStability ?? 0,
            StabilityAfter = result.State.Stability,
            StabilityFastBefore = oldStabilityFast ?? 0,
            StabilityFastAfter = result.State.FastStability,
            DifficultyBefore = oldDifficulty ?? 0,
            DifficultyAfter = result.State.Difficulty,
            DueAtAfter = result.DueAt
        };
        _db.WordReviewEvents.Add(reviewEvent);
        await _db.SaveChangesAsync();

        return new WordProgressDto(newWordProgress.Id, wordId, word.Term, word.Reading, word.Meaning,
    reviewEvent.StabilityAfter, reviewEvent.StabilityFastAfter, reviewEvent.DifficultyAfter,
    reviewEvent.DueAtAfter, reviewEvent.ReviewedAt, newWordProgress.ReviewCount, newWordProgress.LapseCount);
    }

    public async Task<List<Guid>> SelectWordsForGenerationAsync(Guid userId, List<Word> candidateWords,
        DateTimeOffset now, int limit)
    {
        List<Guid> candidateWordIds = candidateWords.Select(w => w.Id).ToList();

        List<WordProgress> candidateWordsProgresses = await _db.WordProgresses
            .AsNoTracking()
            .Where(p => candidateWordIds.Contains(p.WordId) && p.UserId == userId)
            .ToListAsync();

        Dictionary<Guid, WordProgress> progressByWordId = candidateWordsProgresses.ToDictionary(p => p.WordId);

        List<SrsCandidate> candidates = candidateWords.Select(word =>
        {
            WordLearningProgress? progress = progressByWordId.TryGetValue(word.Id, out WordProgress? found)
                ? new WordLearningProgress(
                    found.UserId,
                    found.WordId,
                    new Fsrs7MemoryState((float)found.Stability, (float)found.StabilityFast, (float)found.Difficulty),
                    found.LastReviewedAt,
                    found.DueAt,
                    found.ReviewCount,
                    found.LapseCount)
                : null;

            return new SrsCandidate(userId, word.Id, word.CreatedAt, progress);
        }).ToList();

        IReadOnlyList<SrsCandidate> ordered = SrsRecommendation.OrderForReview(candidates, now, limit);
        return ordered.Select(c => c.WordId).ToList();
    }
}