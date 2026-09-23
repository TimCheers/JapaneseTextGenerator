namespace KotobaApi.Srs.Domain;

/// <summary>Immutable review audit event suitable for later persistence and optimizer training.</summary>
public sealed record ReviewEvent(
    Guid EventId,
    Guid UserId,
    Guid WordId,
    DateTimeOffset ReviewedAt,
    FsrsRating Rating,
    ReviewSource Source,
    float ElapsedDays,
    float? RetrievabilityBeforeReview,
    Fsrs7MemoryState StateAfterReview,
    float DesiredRetention,
    float ScheduledIntervalDays,
    DateTimeOffset DueAt,
    string SchedulerAlgorithm,
    bool WasFirstReview);
