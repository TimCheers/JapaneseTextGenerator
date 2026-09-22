using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class GenerationRequestService : IGenerationRequestService
{
    private readonly AppDbContext _db;
    public GenerationRequestService(AppDbContext db) => _db = db;

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
        var request = new GenerationRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = GenerationStatus.Pending,
            PromptParams = dto.PromptParams,
            CreatedAt = DateTimeOffset.UtcNow,
            Words = dto.WordIds.Select(wordId => new GenerationRequestWord
            {
                Id = Guid.NewGuid(),
                WordId = wordId
            }).ToList()
        };

        _db.GenerationRequests.Add(request);
        await _db.SaveChangesAsync();

        return ToDto(request);
    }

    private static GenerationRequestDto ToDto(GenerationRequest r) =>
        new(r.Id, r.Status.ToString(), r.PromptParams, r.ErrorMessage, r.CreatedAt, r.CompletedAt,
            r.Words.Select(w => w.WordId).ToList());
}
