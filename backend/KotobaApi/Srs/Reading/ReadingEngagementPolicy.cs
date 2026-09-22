namespace KotobaApi.Srs.Reading;

public sealed record ReadingEngagementOptions
{
    /// <summary>
    /// A conservative per-target visibility floor. The client may aggregate multiple visibility spans.
    /// </summary>
    public TimeSpan MinimumTargetVisibleDuration { get; init; } = TimeSpan.FromMilliseconds(1200);

    internal void Validate()
    {
        if (MinimumTargetVisibleDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumTargetVisibleDuration));
        }
    }
}

public sealed record ContextualReadingObservation(
    Guid EventId,
    Guid UserId,
    Guid WordId,
    DateTimeOffset ObservedAt,
    bool TargetWasVisible,
    TimeSpan TargetVisibleDuration,
    bool ReachedEndOrAdvanced);

public enum ContextualReadingDisposition
{
    Applied = 0,
    IgnoredTargetNotVisible = 1,
    IgnoredTooBrief = 2,
    IgnoredNotCompleted = 3,
}

public sealed class ReadingEngagementPolicy
{
    private readonly ReadingEngagementOptions _options;

    public ReadingEngagementPolicy(ReadingEngagementOptions? options = null)
    {
        _options = options ?? new ReadingEngagementOptions();
        _options.Validate();
    }

    public ContextualReadingDisposition Evaluate(ContextualReadingObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        if (!observation.TargetWasVisible)
        {
            return ContextualReadingDisposition.IgnoredTargetNotVisible;
        }

        if (observation.TargetVisibleDuration < _options.MinimumTargetVisibleDuration)
        {
            return ContextualReadingDisposition.IgnoredTooBrief;
        }

        if (!observation.ReachedEndOrAdvanced)
        {
            return ContextualReadingDisposition.IgnoredNotCompleted;
        }

        return ContextualReadingDisposition.Applied;
    }
}
