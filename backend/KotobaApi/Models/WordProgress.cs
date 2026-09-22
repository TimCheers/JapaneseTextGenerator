namespace KotobaApi.Models;

public class WordProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WordId { get; set; }
    public double Stability { get; set; }
    public double StabilityFast { get; set; }
    public double Difficulty { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public int ReviewCount { get; set; }
    public int LapseCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
