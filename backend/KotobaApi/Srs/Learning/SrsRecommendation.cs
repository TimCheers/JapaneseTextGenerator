using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Learning;

/// <summary>
/// Classification of a single word for review recommendation.
/// See docs/tests.md section B.
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
/// NEW ordering uses <see cref="CreatedAt"/>; DUE ordering uses progress DueAt.
/// </summary>
public sealed record SrsCandidate(
    Guid WordId,
    DateTimeOffset CreatedAt,
    WordLearningProgress? Progress);

/// <summary>
/// Pure MVP recommender matching docs/tests.md section D:
/// DUE (DueAt ASC, retrievability ASC, WordId ASC) then NEW (CreatedAt ASC, WordId ASC),
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

        if (candidate.Progress.DueAt is null)
        {
            return SrsRecommendationKind.Invalid;
        }

        return candidate.Progress.DueAt.Value <= now
            ? SrsRecommendationKind.Due
            : SrsRecommendationKind.NotDue;
    }

    /// <param name="retrievabilityByWord">
    /// Optional predicted retrievability at <paramref name="now"/>, keyed by WordId.
    /// Used only to break ties when DueAt values are equal (lower = harder to recall = first).
    /// </param>
    public static IReadOnlyList<SrsCandidate> OrderForReview(
        IEnumerable<SrsCandidate> candidates,
        DateTimeOffset now,
        int limit,
        IReadOnlyDictionary<Guid, double>? retrievabilityByWord = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit cannot be negative.");
        }

        var list = candidates.ToList();

        var duplicates = list.GroupBy(c => c.WordId).FirstOrDefault(g => g.Count() > 1);
        if (duplicates is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate progress for word {duplicates.Key}. Expected UNIQUE (user_id, word_id).");
        }

        foreach (var candidate in list)
        {
            if (Classify(candidate, now) == SrsRecommendationKind.Invalid)
            {
                throw new InvalidOperationException(
                    $"Word {candidate.WordId} has progress with null DueAt. Data integrity error.");
            }
        }

        var due = list
            .Where(c => Classify(c, now) == SrsRecommendationKind.Due)
            .OrderBy(c => c.Progress!.DueAt!.Value)
            .ThenBy(c => retrievabilityByWord != null && retrievabilityByWord.TryGetValue(c.WordId, out var r) ? r : double.MaxValue)
            .ThenBy(c => c.WordId);

        var fresh = list
            .Where(c => Classify(c, now) == SrsRecommendationKind.New)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.WordId);

        return due.Concat(fresh).Take(limit).ToList();
    }
}
