using KotobaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class ComprehensionQuestionService : IComprehensionQuestionService
{
    private readonly AppDbContext _db;
    public ComprehensionQuestionService(AppDbContext db) => _db = db;
    
    public async Task<List<ComprehensionQuestionDto>> GetByGeneratedTextIdAsync(Guid generatedTextId, Guid userId) =>
        await _db.ComprehensionQuestions
            .AsNoTracking()
            .Where(q => q.GeneratedTextId == generatedTextId
                        && q.GeneratedText.GenerationRequest.UserId == userId)
            .Select(q => new ComprehensionQuestionDto(q.Id, q.GeneratedTextId, q.QuestionText, q.Options))
            .ToListAsync();
}
