namespace KotobaApi.Models;

public class PracticeAttempt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GeneratedTextId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int? Score { get; set; }

    public User User { get; set; } = null!;
    public GeneratedText GeneratedText { get; set; } = null!;
    public ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
}
