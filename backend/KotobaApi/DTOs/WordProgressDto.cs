public record WordProgressDto(Guid Id, Guid WordId, double Stability, double StabilityFast, double Difficulty, DateTimeOffset DueAt, DateTimeOffset? LastReviewedAt, int ReviewCount, int LapseCount);
