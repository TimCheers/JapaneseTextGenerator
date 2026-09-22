using KotobaApi.Data;
using KotobaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KotobaApi.Services;

public class DeckService : IDeckService
{
    private readonly AppDbContext _db;
    public DeckService(AppDbContext db) => _db = db;

    public async Task<List<DeckDto>> GetAllForUserAsync(Guid userId) =>
        await _db.Decks
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new DeckDto(d.Id, d.UserId, d.Name, d.Description, d.CreatedAt))
            .ToListAsync();

    public async Task<DeckDto?> GetByIdAsync(Guid userId, Guid id)
    {
        var deck = await _db.Decks.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        return deck is null ? null : new DeckDto(deck.Id, deck.UserId, deck.Name, deck.Description, deck.CreatedAt);
    }

    public async Task<DeckDto> CreateAsync(Guid userId, CreateDeckDto dto)
    {
        var deck = new Deck { Id = Guid.NewGuid(), UserId = userId, Name = dto.Name, Description = dto.Description };
        _db.Decks.Add(deck);
        await _db.SaveChangesAsync();
        return new DeckDto(deck.Id, deck.UserId, deck.Name, deck.Description, deck.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid userId, Guid id, UpdateDeckDto dto)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deck is null) return false;

        deck.Name = dto.Name;
        deck.Description = dto.Description;
        deck.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deck is null) return false;

        _db.Decks.Remove(deck);
        await _db.SaveChangesAsync();
        return true;
    }
}