using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Learning;

/// <summary>
/// Classification of a single word for review recommendation.
/// See docs/srs-scheduling-and-recommendation.md section B.
/// </summary>
public enum SrsRecommendationKind
{
    New = 0,
    Due = 1,
    NotDue = 2,
    Invalid = 3,
}

/// <summary>
/// One word considered for recommendation, with its current progress (if any).
/// When progress is present it must belong to the same user and word.
/// NEW ordering uses <see cref="CreatedAt"/>; DUE ordering uses progress DueAt.
/// </summary>
public sealed record SrsCandidate(
    Guid UserId,
    Guid WordId,
    DateTimeOffset CreatedAt,
    WordLearningProgress? Progress);

/// <summary>
/// Pure MVP recommender matching docs/srs-scheduling-and-recommendation.md section D:
/// DUE (DueAt ASC, WordId ASC) then NEW (CreatedAt ASC, WordId ASC),
/// truncated to limit. NOT_DUE excluded. Invalid progress fails fast.
/// </summary>
public static class SrsRecommendation
{
    public static SrsRecommendationKind Classify(SrsCandidate candidate, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.Progress is null)
        {
            return SrsRecommendationKind.New;
        }

        if (!IsCoherentProgress(candidate))
        {
            return SrsRecommendationKind.Invalid;
        }

        return candidate.Progress.DueAt!.Value <= now
            ? SrsRecommendationKind.Due
            : SrsRecommendationKind.NotDue;
    }

    public static IReadOnlyList<SrsCandidate> OrderForReview(
        IEnumerable<SrsCandidate> candidates,
        DateTimeOffset now,
        int limit)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit cannot be negative.");
        }

        var list = candidates.ToList();

        if (list.Select(c => c.UserId).Distinct().Count() > 1)
        {
            throw new InvalidOperationException("Recommendation candidates must belong to a single user.");
        }

        var duplicates = list.GroupBy(c => (c.UserId, c.WordId)).FirstOrDefault(g => g.Count() > 1);
        if (duplicates is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate progress for user {duplicates.Key.UserId}, word {duplicates.Key.WordId}. Expected UNIQUE (user_id, word_id).");
        }

        foreach (var candidate in list)
        {
            if (Classify(candidate, now) == SrsRecommendationKind.Invalid)
            {
                throw new InvalidOperationException(
                    $"Word {candidate.WordId} has incoherent progress. Data integrity error.");
            }
        }

        var due = list
            .Where(c => Classify(c, now) == SrsRecommendationKind.Due)
            .OrderBy(c => c.Progress!.DueAt!.Value)
            .ThenBy(c => c.WordId);

        var fresh = list
            .Where(c => Classify(c, now) == SrsRecommendationKind.New)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.WordId);

        return due.Concat(fresh).Take(limit).ToList();
    }

    /// <summary>
    /// A stored progress row is coherent only if it carries a complete scheduling
    /// state for the same user and word. <see cref="InMemoryLearningEngine"/> never
    /// produces anything else; this guards the future database-backed loader.
    /// </summary>
    private static bool IsCoherentProgress(SrsCandidate candidate)
    {
        var progress = candidate.Progress;
        if (progress is null)
        {
            return true;
        }

        return progress.UserId == candidate.UserId
            && progress.WordId == candidate.WordId
            && progress.MemoryState is not null
            && progress.LastReviewedAt is not null
            && progress.DueAt is not null
            && progress.ReviewCount > 0;
    }
}
