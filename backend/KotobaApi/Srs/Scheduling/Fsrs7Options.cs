namespace KotobaApi.Srs.Scheduling;

public sealed record Fsrs7Options
{
    public float DesiredRetention { get; init; } = 0.90f;
    public Fsrs7Parameters Parameters { get; init; } = Fsrs7Parameters.Default;

    internal void Validate()
    {
        if (!float.IsFinite(DesiredRetention) || DesiredRetention is <= 0.0f or >= 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(DesiredRetention), "Desired retention must be between 0 and 1.");
        }

        ArgumentNullException.ThrowIfNull(Parameters);
    }
}
