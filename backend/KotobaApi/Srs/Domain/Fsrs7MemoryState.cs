namespace KotobaApi.Srs.Domain;

/// <summary>
/// FSRS-7 dual-trace memory state. Stability values are expressed in days.
/// </summary>
public readonly record struct Fsrs7MemoryState(
    double Stability,
    double FastStability,
    double Difficulty);
