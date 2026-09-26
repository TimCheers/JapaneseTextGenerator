using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class GenerationRequestService : IGenerationRequestService
{
    private readonly AppDbContext _db;
    private readonly IWordService _wordService;
    private readonly IAiTextGenerationService _aiTextGenerationService;

    public GenerationRequestService(AppDbContext db, IWordService wordService,
        IAiTextGenerationService aiTextGenerationService)
    {
        _db = db;
        _wordService = wordService;
        _aiTextGenerationService = aiTextGenerationService;
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
        var words = await _wordService.GetAllForUserAsync(userId);
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
            string content =  await _aiTextGenerationService.GenerateTextAsync(words);
            request.Status = GenerationStatus.Completed;
            request.CompletedAt = DateTimeOffset.UtcNow;
            _db.GeneratedTexts.Add(new GeneratedText
            {
                Id = Guid.NewGuid(), GenerationRequestId = request.Id, Content = content,
                CreatedAt = DateTimeOffset.UtcNow
            });

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