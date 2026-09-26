using KotobaApi.Models;

namespace KotobaApi.Services;

public interface IAiTextGenerationService
{
    Task<string> GenerateTextAsync(List<Word> words);
}