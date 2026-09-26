using System.Net.Http.Headers;
using KotobaApi.Models;
using System.Text.Json.Serialization;

namespace KotobaApi.Services;

public record GeminiRoleParts(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("parts")] List<GeminiText> Parts
);

public record GeminiParts(
    [property: JsonPropertyName("parts")] List<GeminiText> Parts
);

public record GeminiText(
    [property: JsonPropertyName("text")] string Text
);

public record GeminiContent(
    [property: JsonPropertyName("content")]
    GeminiParts Content
);

public record GeminiRequest(
    [property: JsonPropertyName("contents")]
    List<GeminiRoleParts> Contents,
    [property: JsonPropertyName("systemInstruction")]
    GeminiParts SystemInstruction
);

public record GeminiResponse(
    [property: JsonPropertyName("candidates")]
    List<GeminiContent> Candidates
);

public class GeminiTextGenerationService : IAiTextGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public GeminiTextGenerationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _config = configuration;
    }

    public async Task<string> GenerateTextAsync(List<Word> words)
    {
        string? apiKey = _config["Gemini:ApiKey"] ??
                         throw new InvalidOperationException("Gemini:ApiKey is not configured");
        string? model = _config["Gemini:Model"] ??
                        throw new InvalidOperationException("Gemini:Model is not configured");

        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        string wordsPrompt = string.Join("\n", words.Select(w => $"Word: {w.Term}\tMeaning: {w.Meaning}"));

        GeminiRequest request = new GeminiRequest(
            new List<GeminiRoleParts>
                { new GeminiRoleParts("user", new List<GeminiText> { new GeminiText(wordsPrompt) }) },
            new GeminiParts(new List<GeminiText>
            {
                new GeminiText(
                    "You are generating a short reading practice text in Japanese for a language learner. Output ONLY the Japanese text itself, using the given words naturally in context. Do not include readings, romaji, translations, explanations, headers, or any markdown formatting — just the raw Japanese sentences.")
            }));

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent", request);
        response.EnsureSuccessStatusCode();

        GeminiResponse? parsedResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

        return parsedResponse?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text ??
               throw new InvalidOperationException("Gemini returned no content");
    }
}