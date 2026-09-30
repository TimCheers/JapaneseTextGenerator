using System.Net.Http.Headers;
using System.Text.Json;
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

        string wordsPrompt = string.Join("\n", words.Select(w => $"Word: {w.Term}\tMeaning: {w.Meaning}"));

        GeminiRequest request = new GeminiRequest(
            new List<GeminiRoleParts>
                { new GeminiRoleParts("user", new List<GeminiText> { new GeminiText(wordsPrompt) }) },
            new GeminiParts(new List<GeminiText>
            {
                new GeminiText(
                    "You are generating a short reading practice text in Japanese for a language learner. Output ONLY the Japanese text itself, using the given words naturally in context. Do not include readings, romaji, translations, explanations, headers, or any markdown formatting — just the raw Japanese sentences.")
            }));

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("x-goog-api-key", apiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Gemini request failed ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
        }

        GeminiResponse? parsedResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

        return parsedResponse?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text ??
               throw new InvalidOperationException("Gemini returned no content");
    }

    public async Task<List<ComprehensionQuestionSeed>> GenerateComprehensionQuestionsAsync(string text, int count)
    {
        string? apiKey = _config["Gemini:ApiKey"] ??
                         throw new InvalidOperationException("Gemini:ApiKey is not configured");
        string? model = _config["Gemini:Model"] ??
                        throw new InvalidOperationException("Gemini:Model is not configured");

        string systemPrompt =
            $"You will receive a short Japanese text. Write exactly {count} multiple-choice reading-comprehension " +
            "questions about it, in Japanese. Each question must have exactly 4 options, and exactly one correct " +
            "answer copied verbatim from its options. Respond with ONLY a raw JSON array (no markdown, no code " +
            "fences, no explanations) in this exact shape: " +
            "[{\"question\":\"...\",\"options\":[\"...\",\"...\",\"...\",\"...\"],\"correctAnswer\":\"...\"}]";

        GeminiRequest request = new GeminiRequest(
            new List<GeminiRoleParts>
                { new GeminiRoleParts("user", new List<GeminiText> { new GeminiText(text) }) },
            new GeminiParts(new List<GeminiText> { new GeminiText(systemPrompt) }));

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("x-goog-api-key", apiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Gemini request failed ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
        }

        GeminiResponse? parsedResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        string raw = parsedResponse?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text ??
                     throw new InvalidOperationException("Gemini returned no content");

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