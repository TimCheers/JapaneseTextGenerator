// Без CorrectAnswer — это ровно то, что должен видеть пользователь до ответа.
public record ComprehensionQuestionDto(Guid Id, Guid GeneratedTextId, string QuestionText, string Options);
