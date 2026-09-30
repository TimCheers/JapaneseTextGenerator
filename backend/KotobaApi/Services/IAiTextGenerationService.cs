using System.Text.Json.Serialization;
using KotobaApi.Models;

namespace KotobaApi.Services;

public record ComprehensionQuestionSeed(
    [property: JsonPropertyName("question")] string QuestionText,
    [property: JsonPropertyName("options")] List<string> Options,
    [property: JsonPropertyName("correctAnswer")] string CorrectAnswer);

public interface IAiTextGenerationService
{
    Task<string> GenerateTextAsync(List<Word> words);
    Task<List<ComprehensionQuestionSeed>> GenerateComprehensionQuestionsAsync(string text, int count);
}