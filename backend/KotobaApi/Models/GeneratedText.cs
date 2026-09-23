namespace KotobaApi.Models;

public class GeneratedText
{
    public Guid Id { get; set; }
    public Guid GenerationRequestId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public GenerationRequest GenerationRequest { get; set; } = null!;
    public ICollection<GeneratedTextWordUsage> WordUsages { get; set; } = new List<GeneratedTextWordUsage>();
    public ICollection<ComprehensionQuestion> Questions { get; set; } = new List<ComprehensionQuestion>();
    public ICollection<PracticeAttempt> PracticeAttempts { get; set; } = new List<PracticeAttempt>();
}
