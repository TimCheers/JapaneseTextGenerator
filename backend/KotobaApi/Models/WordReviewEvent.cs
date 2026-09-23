namespace KotobaApi.Models;

public enum FsrsRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4
}

public enum ReviewSource
{
    ExplicitRating,
    ContextualReading,
    MeaningReveal
}

public class WordReviewEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid WordId { get; set; }
    public FsrsRating Rating { get; set; }
    public ReviewSource Source { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
    public int ElapsedSeconds { get; set; }
    public double DesiredRetention { get; set; }
    public string SchedulerVersion { get; set; } = string.Empty;
    public double StabilityBefore { get; set; }
    public double StabilityAfter { get; set; }
    public double StabilityFastBefore { get; set; }
    public double StabilityFastAfter { get; set; }
    public double DifficultyBefore { get; set; }
    public double DifficultyAfter { get; set; }
    public DateTimeOffset DueAtAfter { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
