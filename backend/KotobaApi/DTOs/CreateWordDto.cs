using KotobaApi.Models;

public record CreateWordDto(string Term, string? Reading, string Meaning, PartOfSpeech? PartOfSpeech, JlptLevel? JlptLevel, string? ExampleSentence, string? Notes, string? AcquisitionSource);