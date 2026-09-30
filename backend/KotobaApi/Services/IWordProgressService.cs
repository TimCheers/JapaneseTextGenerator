using KotobaApi.Models;
using KotobaApi.Srs.Domain;

namespace KotobaApi.Services;

public interface IWordProgressService
{
    Task<List<WordProgressDto>> GetDueForUserAsync(Guid userId, DateTimeOffset asOf);

    Task<WordProgressDto> ApplyReviewAsync(Guid userId, Guid wordId, Guid eventId, FsrsRating rating,
        ReviewSource source, DateTimeOffset reviewedAt);

    Task<List<Guid>> SelectWordsForGenerationAsync(Guid userId, List<Word> candidateWords, DateTimeOffset now,
        int limit);
}