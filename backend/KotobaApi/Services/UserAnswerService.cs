using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class UserAnswerService : IUserAnswerService
{
    private readonly AppDbContext _db;
    public UserAnswerService(AppDbContext db) => _db = db;

    public async Task<List<UserAnswerDto>> GetByAttemptIdAsync(Guid practiceAttemptId, Guid userId) =>
        await _db.UserAnswers
            .AsNoTracking()
            .Where(a => a.PracticeAttemptId == practiceAttemptId && a.PracticeAttempt.UserId == userId)
            .Select(a => new UserAnswerDto(a.Id, a.ComprehensionQuestionId, a.SelectedAnswer, a.IsCorrect, a.AnsweredAt))
            .ToListAsync();
    
    public async Task<UserAnswerDto?> SubmitAsync(Guid practiceAttemptId, Guid userId, SubmitAnswerDto dto)
    {
        var attempt = await _db.PracticeAttempts
            .FirstOrDefaultAsync(a => a.Id == practiceAttemptId && a.UserId == userId);
        if (attempt is null) return null;

        var question = await _db.ComprehensionQuestions
            .FirstOrDefaultAsync(q => q.Id == dto.ComprehensionQuestionId);
        if (question is null) return null;

        var answer = new UserAnswer
        {
            Id = Guid.NewGuid(),
            PracticeAttemptId = practiceAttemptId,
            ComprehensionQuestionId = dto.ComprehensionQuestionId,
            SelectedAnswer = dto.SelectedAnswer,
            IsCorrect = dto.SelectedAnswer == question.CorrectAnswer,
            AnsweredAt = DateTimeOffset.UtcNow
        };

        _db.UserAnswers.Add(answer);
        await _db.SaveChangesAsync();

        return new UserAnswerDto(answer.Id, answer.ComprehensionQuestionId, answer.SelectedAnswer, answer.IsCorrect, answer.AnsweredAt);
    }
}
