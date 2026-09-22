namespace KotobaApi.Srs.Domain;

/// <summary>Current global memory state for one user and one learned lexical item.</summary>
public sealed record WordLearningProgress(
    Guid UserId,
    Guid WordId,
    Fsrs7MemoryState? MemoryState,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset? DueAt,
    int ReviewCount,
    int LapseCount)
{
    public static WordLearningProgress New(Guid userId, Guid wordId) =>
        new(userId, wordId, null, null, null, 0, 0);
}
