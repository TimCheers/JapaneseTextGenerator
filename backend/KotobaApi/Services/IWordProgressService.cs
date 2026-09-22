namespace KotobaApi.Services;

public interface IWordProgressService
{
    Task<List<WordProgressDto>> GetDueForUserAsync(Guid userId, DateTimeOffset asOf);
}
