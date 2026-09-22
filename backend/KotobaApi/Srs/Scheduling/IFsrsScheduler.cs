using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Scheduling;

public sealed record FsrsScheduleRequest(
    Fsrs7MemoryState? PreviousState,
    DateTimeOffset? LastReviewedAt,
    DateTimeOffset ReviewedAt,
    FsrsRating Rating,
    double? DesiredRetention = null);

public sealed record FsrsScheduleResult(
    Fsrs7MemoryState State,
    double ElapsedDays,
    double? RetrievabilityBeforeReview,
    double DesiredRetention,
    double IntervalDays,
    DateTimeOffset DueAt,
    bool WasFirstReview);

public interface IFsrsScheduler
{
    string AlgorithmId { get; }
    FsrsScheduleResult Schedule(FsrsScheduleRequest request);
    double Retrievability(Fsrs7MemoryState state, TimeSpan elapsed);
    double IntervalAtRetention(Fsrs7MemoryState state, double desiredRetention);
}
