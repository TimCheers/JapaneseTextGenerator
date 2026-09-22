namespace KotobaApi.Models;

public enum GenerationStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

// Заявка пользователя на генерацию текста по выбранным словам.
// Сам текст создаётся асинхронно фоновым сервисом генерации (ещё не
// написан) — здесь только параметры запроса и его статус.
public class GenerationRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
    public string? PromptParams { get; set; } // jsonb: длина текста, тема, уровень сложности и т.п.
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<GenerationRequestWord> Words { get; set; } = new List<GenerationRequestWord>();
    public GeneratedText? GeneratedText { get; set; }
}
