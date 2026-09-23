public record GenerationRequestDto(
    Guid Id,
    string Status,
    string? PromptParams,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    List<Guid> WordIds);

public record CreateGenerationRequestDto(string? PromptParams, List<Guid> WordIds);
