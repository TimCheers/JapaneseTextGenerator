namespace KotobaApi.Srs.Scheduling;

public sealed record Fsrs7Options
{
    public double DesiredRetention { get; init; } = 0.90;
    public Fsrs7Parameters Parameters { get; init; } = Fsrs7Parameters.Default;

    internal void Validate()
    {
        if (!double.IsFinite(DesiredRetention) || DesiredRetention is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DesiredRetention), "Desired retention must be between 0 and 1.");
        }

        ArgumentNullException.ThrowIfNull(Parameters);
    }
}
