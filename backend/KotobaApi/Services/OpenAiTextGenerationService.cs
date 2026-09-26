using System.Net.Http.Headers;
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
}