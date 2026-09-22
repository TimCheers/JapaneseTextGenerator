using KotobaApi.Models;

public record WordDto(Guid Id, Guid DeckId, string Term, string? Reading, string Meaning, PartOfSpeech? PartOfSpeech, JlptLevel? JlptLevel,string? ExampleSentence, string? Notes, string? AcquisitionSource,  DateTimeOffset CreatedAt);