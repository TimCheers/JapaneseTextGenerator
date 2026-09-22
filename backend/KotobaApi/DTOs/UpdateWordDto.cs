using KotobaApi.Models;

public record UpdateWordDto(string Term, string? Reading, string Meaning, PartOfSpeech? PartOfSpeech, JlptLevel? JlptLevel, string? ExampleSentence, string? Notes, string? AcquisitionSource);