using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class WordService : IWordService
{
    private readonly AppDbContext _db;
    public WordService(AppDbContext db) => _db = db;
    
    public async Task<List<WordDto>> GetAllForDeckAsync(Guid deckId) =>
        await _db.Words
            .AsNoTracking()
            .Where(d => d.DeckId == deckId)
            .Select(d => new WordDto(d.Id, d.DeckId, d.Term, d.Reading, d.Meaning, d.PartOfSpeech, d.JlptLevel, d.ExampleSentence, d.Notes, d.AcquisitionSource, d.CreatedAt))
            .ToListAsync();

    public async Task<WordDto?> GetByIdAsync(Guid deckId, Guid id)
    {
        var word = await _db.Words.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.DeckId == deckId);
        return word is null ? null : new WordDto(word.Id, word.DeckId, word.Term, word.Reading, word.Meaning, word.PartOfSpeech, word.JlptLevel, word.ExampleSentence, word.Notes, word.AcquisitionSource, word.CreatedAt);
    }

    public async Task<WordDto> CreateAsync(Guid deckId, CreateWordDto dto)
    {
        var word = new Word { Id = Guid.NewGuid(), DeckId = deckId, Term = dto.Term, Reading = dto.Reading, Meaning = dto.Meaning, PartOfSpeech = dto.PartOfSpeech, JlptLevel = dto.JlptLevel, ExampleSentence = dto.ExampleSentence, Notes = dto.Notes, AcquisitionSource = dto.AcquisitionSource};
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        return new WordDto(word.Id, word.DeckId, word.Term, word.Reading, word.Meaning, word.PartOfSpeech, word.JlptLevel, word.ExampleSentence, word.Notes, word.AcquisitionSource, word.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid deckId, Guid id, UpdateWordDto dto)
    {
        var word = await _db.Words.FirstOrDefaultAsync(d => d.Id == id && d.DeckId == deckId);
        if (word is null) return false;

        word.Term = dto.Term;
        word.Reading = dto.Reading;
        word.Meaning = dto.Meaning;
        word.PartOfSpeech = dto.PartOfSpeech;
        word.JlptLevel = dto.JlptLevel;
        word.ExampleSentence = dto.ExampleSentence;
        word.Notes = dto.Notes;
        word.AcquisitionSource = dto.AcquisitionSource;
        word.Reading = dto.Reading;
        word.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid deckId, Guid id)
    {
        var word = await _db.Words.FirstOrDefaultAsync(d => d.Id == id && d.DeckId == deckId);
        if (word is null) return false;

        _db.Words.Remove(word);
        await _db.SaveChangesAsync();
        return true;
    }
}