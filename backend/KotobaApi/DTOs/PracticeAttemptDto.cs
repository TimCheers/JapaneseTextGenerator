public record PracticeAttemptDto(Guid Id, Guid GeneratedTextId, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int? Score);
