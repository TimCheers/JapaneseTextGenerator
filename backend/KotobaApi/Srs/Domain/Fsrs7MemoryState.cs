namespace KotobaApi.Srs.Domain;

/// <summary>
/// FSRS-7 dual-trace memory state. Stability values are expressed in days.
/// Single-precision floats mirror the Rust f32 runtime of fsrs-rs.
/// </summary>
public readonly record struct Fsrs7MemoryState(
    float Stability,
    float FastStability,
    float Difficulty);
