namespace KotobaApi.Srs.Domain;

/// <summary>Immutable review audit event suitable for later persistence and optimizer training.</summary>
public sealed record ReviewEvent(
    Guid EventId,
    Guid UserId,
    Guid WordId,
    DateTimeOffset ReviewedAt,
    FsrsRating Rating,
    ReviewSource Source,
    double ElapsedDays,
    double? RetrievabilityBeforeReview,
    Fsrs7MemoryState StateAfterReview,
    double DesiredRetention,
    double ScheduledIntervalDays,
    DateTimeOffset DueAt,
    string SchedulerAlgorithm,
    bool WasFirstReview);
