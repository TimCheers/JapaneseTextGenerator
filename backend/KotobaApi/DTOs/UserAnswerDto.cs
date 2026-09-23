public record UserAnswerDto(Guid Id, Guid ComprehensionQuestionId, string SelectedAnswer, bool IsCorrect, DateTimeOffset AnsweredAt);

public record SubmitAnswerDto(Guid ComprehensionQuestionId, string SelectedAnswer);
