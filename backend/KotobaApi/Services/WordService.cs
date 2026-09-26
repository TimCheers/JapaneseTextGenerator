using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace KotobaApi.Services;

public class WordService : IWordService
{
    private readonly AppDbContext _db;
    public WordService(AppDbContext db) => _db = db;

    public async Task<List<WordDto>> GetAllForDeckAsync(Guid deckId) =>
        await _db.Words
            .AsNoTracking()
            .Where(d => d.DeckId == deckId)
            .Select(d => new WordDto(d.Id, d.DeckId, d.Term, d.Reading, d.Meaning, d.PartOfSpeech, d.JlptLevel,
                d.ExampleSentence, d.Notes, d.AcquisitionSource, d.CreatedAt))
            .ToListAsync();

    public async Task<List<Word>> GetAllForUserAsync(Guid userId) =>
        await _db.Words
            .AsNoTracking()
            .Where(w => w.Deck.UserId == userId)
            .ToListAsync();

    public async Task<WordDto?> GetByIdAsync(Guid deckId, Guid id)
    {
        var word = await _db.Words.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.DeckId == deckId);
        return word is null
            ? null
            : new WordDto(word.Id, word.DeckId, word.Term, word.Reading, word.Meaning, word.PartOfSpeech,
                word.JlptLevel, word.ExampleSentence, word.Notes, word.AcquisitionSource, word.CreatedAt);
    }

    public async Task<WordDto> CreateAsync(Guid deckId, CreateWordDto dto)
    {
        var word = new Word
        {
            Id = Guid.NewGuid(), DeckId = deckId, Term = dto.Term, Reading = dto.Reading, Meaning = dto.Meaning,
            PartOfSpeech = dto.PartOfSpeech, JlptLevel = dto.JlptLevel, ExampleSentence = dto.ExampleSentence,
            Notes = dto.Notes, AcquisitionSource = dto.AcquisitionSource
        };
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        return new WordDto(word.Id, word.DeckId, word.Term, word.Reading, word.Meaning, word.PartOfSpeech,
            word.JlptLevel, word.ExampleSentence, word.Notes, word.AcquisitionSource, word.CreatedAt);
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

    public async Task<List<WordDto>> ImportFromExcelAsync(Guid deckId, Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        List<WordDto> newWords = new List<WordDto>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var term = row.Cell(1).GetString();
            var reading = row.Cell(2).GetString();
            var meaning = row.Cell(3).GetString();
            var exampleSentence = row.Cell(4).GetString();

            var word = new Word
            {
                Id = Guid.NewGuid(), DeckId = deckId, Term = term, Reading = reading,
                Meaning = meaning, ExampleSentence = exampleSentence,
            }; // Что делать с partOfSpeech и jlptLevel
            _db.Words.Add(word);
            newWords.Add(new WordDto(word.Id, word.DeckId, word.Term, word.Reading, word.Meaning, null,
                null, word.ExampleSentence, null, null, word.CreatedAt));
        }

        await _db.SaveChangesAsync();
        return newWords;
    }
}