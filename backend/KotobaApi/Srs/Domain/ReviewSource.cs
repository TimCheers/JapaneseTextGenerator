namespace KotobaApi.Srs.Domain;

/// <summary>
/// Records why a grade was emitted. Keeping provenance separate from the FSRS grade
/// lets future parameter training distinguish contextual exposure from explicit recall.
/// </summary>
public enum ReviewSource
{
    ContextualReading = 1,
    MeaningRevealed = 2,
    ExplicitRating = 3,
    NewWordAssessment = 4,
}
