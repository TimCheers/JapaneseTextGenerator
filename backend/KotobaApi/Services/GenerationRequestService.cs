using System.Text.Json;
using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;
using KotobaApi.Authorization;


namespace KotobaApi.Services;

public class GenerationRequestService : IGenerationRequestService
{
    private const int WordsPerGeneration = 10;
    private const int ComprehensionQuestionsPerText = 3;

    private readonly AppDbContext _db;
    private readonly IWordService _wordService;
    private readonly IWordProgressService _wordProgressService;
    private readonly IAiTextGenerationService _aiTextGenerationService;

    public GenerationRequestService(AppDbContext db, IWordService wordService,
        IAiTextGenerationService aiTextGenerationService, IWordProgressService wordProgressService)
    {
        _db = db;
        _wordService = wordService;
        _aiTextGenerationService = aiTextGenerationService;
        _wordProgressService = wordProgressService;
    }

    public async Task<GenerationRequestDto?> GetByIdAsync(Guid id, Guid userId)
    {
        var request = await _db.GenerationRequests
            .AsNoTracking()
            .Include(r => r.Words)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        return request is null ? null : ToDto(request);
    }

    public async Task<List<GenerationRequestDto>> GetAllForUserAsync(Guid userId)
    {
        var requests = await _db.GenerationRequests
            .AsNoTracking()
            .Include(r => r.Words)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(ToDto).ToList();
    }

    public async Task<GenerationRequestDto> CreateAsync(Guid userId, CreateGenerationRequestDto dto)
    {
        List<Word> allWords = await _wordService.GetAllForUserAsync(userId);

        List<Guid> selectedWordIds = await _wordProgressService.SelectWordsForGenerationAsync(
            userId, allWords, DateTimeOffset.UtcNow, WordsPerGeneration);

        Dictionary<Guid, Word> allWordsById = allWords.ToDictionary(w => w.Id);
        List<Word> words = selectedWordIds
            .Where(allWordsById.ContainsKey)
            .Select(id => allWordsById[id])
            .ToList();

        var request = new GenerationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = GenerationStatus.Pending,
            PromptParams = dto.PromptParams,
            CreatedAt = DateTimeOffset.UtcNow,
            Words = words.Select(w => new GenerationRequestWord
                {
                    Id = Guid.NewGuid(),
                    WordId = w.Id
                }
            ).ToList()
        };

        try
        {
            string content = await _aiTextGenerationService.GenerateTextAsync(words);
            request.Status = GenerationStatus.Completed;
            request.CompletedAt = DateTimeOffset.UtcNow;

            var generatedText = new GeneratedText
            {
                Id = Guid.NewGuid(), GenerationRequestId = request.Id, Content = content,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.GeneratedTexts.Add(generatedText);

            try
            {
                List<ComprehensionQuestionSeed> seeds = await _aiTextGenerationService
                    .GenerateComprehensionQuestionsAsync(content, ComprehensionQuestionsPerText);

                foreach (ComprehensionQuestionSeed seed in seeds)
                {
                    _db.ComprehensionQuestions.Add(new ComprehensionQuestion
                    {
                        Id = Guid.NewGuid(),
                        GeneratedTextId = generatedText.Id,
                        QuestionText = seed.QuestionText,
                        Options = JsonSerializer.Serialize(seed.Options),
                        CorrectAnswer = seed.CorrectAnswer,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception e)
        {
            request.Status = GenerationStatus.Failed;
            request.CompletedAt = DateTimeOffset.UtcNow;
            request.ErrorMessage = e.Message;
        }

        _db.GenerationRequests.Add(request);
        await _db.SaveChangesAsync();

        return ToDto(request);
    }

    private static GenerationRequestDto ToDto(GenerationRequest r) =>
        new(r.Id, r.Status.ToString(), r.PromptParams, r.ErrorMessage, r.CreatedAt, r.CompletedAt,
            r.Words.Select(w => w.WordId).ToList());
}