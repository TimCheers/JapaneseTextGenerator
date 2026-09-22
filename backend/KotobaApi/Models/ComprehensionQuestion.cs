namespace KotobaApi.Models;

public class ComprehensionQuestion
{
    public Guid Id { get; set; }
    public Guid GeneratedTextId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty; // jsonb: варианты ответа
    public string CorrectAnswer { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public GeneratedText GeneratedText { get; set; } = null!;
    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
