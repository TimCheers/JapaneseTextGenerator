using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Scheduling;

public sealed record FsrsScheduleRequest(
    Fsrs7MemoryState? PreviousState,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset ReviewedAt,
    FsrsRating Rating,
    float? DesiredRetention = null);

public sealed record FsrsScheduleResult(
    Fsrs7MemoryState State,
    float ElapsedDays,
    float? RetrievabilityBeforeReview,
    float DesiredRetention,
    float IntervalDays,
    DateTimeOffset DueAt,
    bool WasFirstReview);

public interface IFsrsScheduler
{
    string AlgorithmId { get; }
    FsrsScheduleResult Schedule(FsrsScheduleRequest request);
    float Retrievability(Fsrs7MemoryState state, TimeSpan elapsed);
    float IntervalAtRetention(Fsrs7MemoryState state, float desiredRetention);
}
