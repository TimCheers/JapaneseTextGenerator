using System.Net.Http.Headers;
using System.Text.Json;
using KotobaApi.Models;
using System.Text.Json.Serialization;

namespace KotobaApi.Services;

public record OpenAiMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")]
    string Content
);

public record OpenAiChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")]
    List<OpenAiMessage> Messages
);

public record OpenAiChoice(
    [property: JsonPropertyName("message")]
    OpenAiMessage Message
);

public record OpenAiChatResponse(
    [property: JsonPropertyName("choices")]
    List<OpenAiChoice> Choices
);

public class OpenAiTextGenerationService : IAiTextGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OpenAiTextGenerationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _config = configuration;
    }

    public async Task<string> GenerateTextAsync(List<Word> words)
    {
        string? apiKey = _config["OpenAI:ApiKey"] ??
                         throw new InvalidOperationException("OpenAI:ApiKey is not configured");
        string? model = _config["OpenAI:Model"] ??
                        throw new InvalidOperationException("OpenAI:Model is not configured");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        string wordsPrompt = string.Join("\n", words.Select(w => $"Word: {w.Term}\tMeaning: {w.Meaning}"));

        OpenAiChatRequest request =
            new OpenAiChatRequest(model, new List<OpenAiMessage>
            {
                new OpenAiMessage("system", "Write a short text in Japanese using these words:"),
                new OpenAiMessage("user", wordsPrompt)
            });

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        OpenAiChatResponse? parsedResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();

        return parsedResponse?.Choices.FirstOrDefault()?.Message.Content ??
               throw new InvalidOperationException("OpenAI returned no content");
    }

    public async Task<List<ComprehensionQuestionSeed>> GenerateComprehensionQuestionsAsync(string text, int count)
    {
        string? apiKey = _config["OpenAI:ApiKey"] ??
                         throw new InvalidOperationException("OpenAI:ApiKey is not configured");
        string? model = _config["OpenAI:Model"] ??
                        throw new InvalidOperationException("OpenAI:Model is not configured");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        string systemPrompt =
            $"You will receive a short Japanese text. Write exactly {count} multiple-choice reading-comprehension " +
            "questions about it, in Japanese. Each question must have exactly 4 options, and exactly one correct " +
            "answer copied verbatim from its options. Respond with ONLY a raw JSON array (no markdown, no code " +
            "fences, no explanations) in this exact shape: " +
            "[{\"question\":\"...\",\"options\":[\"...\",\"...\",\"...\",\"...\"],\"correctAnswer\":\"...\"}]";

        OpenAiChatRequest request = new OpenAiChatRequest(model, new List<OpenAiMessage>
        {
            new OpenAiMessage("system", systemPrompt),
            new OpenAiMessage("user", text)
        });

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        OpenAiChatResponse? parsedResponse = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
        string raw = parsedResponse?.Choices.FirstOrDefault()?.Message.Content ??
                     throw new InvalidOperationException("OpenAI returned no content");

        List<ComprehensionQuestionSeed>? seeds = JsonSerializer.Deserialize<List<ComprehensionQuestionSeed>>(
            StripCodeFence(raw), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return seeds ?? [];
    }

    private static string StripCodeFence(string text)
    {
        string trimmed = text.Trim();
        if (!trimmed.StartsWith("```")) return trimmed;

        int firstNewLine = trimmed.IndexOf('\n');
        int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? trimmed[(firstNewLine + 1)..lastFence].Trim()
            : trimmed;
    }
}