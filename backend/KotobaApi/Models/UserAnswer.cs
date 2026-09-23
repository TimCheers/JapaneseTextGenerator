namespace KotobaApi.Models;

public class UserAnswer
{
    public Guid Id { get; set; }
    public Guid PracticeAttemptId { get; set; }
    public Guid ComprehensionQuestionId { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;

    public PracticeAttempt PracticeAttempt { get; set; } = null!;
    public ComprehensionQuestion ComprehensionQuestion { get; set; } = null!;
}
