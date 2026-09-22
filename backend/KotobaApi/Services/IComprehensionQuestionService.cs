namespace KotobaApi.Services;

public interface IComprehensionQuestionService
{
    Task<List<ComprehensionQuestionDto>> GetByGeneratedTextIdAsync(Guid generatedTextId, Guid userId);
}
