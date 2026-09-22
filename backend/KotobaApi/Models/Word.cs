namespace KotobaApi.Models;

public class Word
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public string Term { get; set; } = string.Empty;
    public string? Reading { get; set; }
    public string Meaning { get; set; } = string.Empty;
    public PartOfSpeech? PartOfSpeech { get; set; }
    public JlptLevel? JlptLevel { get; set; }
    public string? ExampleSentence { get; set; }
    public string? Notes { get; set; }
    public string? AcquisitionSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Deck Deck { get; set; } = null!;
}