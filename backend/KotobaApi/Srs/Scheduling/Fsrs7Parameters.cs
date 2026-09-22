namespace KotobaApi.Srs.Scheduling;

/// <summary>Immutable validated FSRS-7 parameter vector (34 parameters).</summary>
public sealed class Fsrs7Parameters
{
    public const int ParameterCount = 34;

    private static readonly double[] DefaultParameterValues =
    [
        0.1104, 2.2395, 3.9221, 11.7841,
        6.1686, 0.6457, 3.6807,
        1.9795, 0.0, 1.3826, 0.7024, 0.5999, 0.8146, 0.6398, 1.0,
        1.3207, 0.6707, 3.8668, 0.4416, 0.0934, 1.8631, 0.6162, 1.0869,
        0.1567, 0.0801, 0.2421, 0.9464, 0.1433, 0.7145, 0.0, 0.5667, 0.3734, 0.5333, 0.3048,
    ];

    // Runtime-valid ranges mirror the reference parameter clipper. An optimizer should
    // clip before constructing this type; runtime code fails fast rather than silently changing a model.
    private static readonly double[] LowerBounds =
    [
        0.0001, 0.0001, 0.0001, 0.0001, 1.0, 0.001, 0.1, 0.0, 0.0, 0.3, 0.01, 0.1,
        0.0, 0.0, 1.0, 0.0, 0.0, 0.5, 0.001, 0.001, 0.0, 0.0, 1.0, 0.01, 0.01, 0.2,
        0.5, 0.01, 0.1, 0.0, 0.1, 0.0, 0.0, 0.0,
    ];

    private static readonly double[] UpperBounds =
    [
        50.0, 100.0, 100.0, 100.0, 10.0, 4.0, 4.0, 4.0, 1.2, 3.0, 1.5, 1.0,
        3.5, 1.0, 7.0, 4.0, 2.0, 6.0, 1.5, 1.0, 5.0, 1.0, 7.0, 0.25, 0.95, 0.85,
        0.99, 1.0, 1.0, 0.9, 1.1, 1.0, 0.6, 0.6,
    ];

    private readonly double[] _values;

    private Fsrs7Parameters(double[] values) => _values = values;

    public static Fsrs7Parameters Default { get; } = new((double[])DefaultParameterValues.Clone());

    public double this[int index] => _values[index];

    public int Count => _values.Length;

    public static Fsrs7Parameters FromOptimized(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();

        if (copy.Length != ParameterCount)
        {
            throw new ArgumentException($"FSRS-7 requires exactly {ParameterCount} parameters.", nameof(values));
        }

        for (var i = 0; i < copy.Length; i++)
        {
            if (!double.IsFinite(copy[i]) || copy[i] < LowerBounds[i] || copy[i] > UpperBounds[i])
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    $"Parameter {i} must be finite and in [{LowerBounds[i]}, {UpperBounds[i]}], but was {copy[i]}.");
            }
        }

        if (copy[1] < copy[0] || copy[2] < copy[1] || copy[3] < copy[2])
        {
            throw new ArgumentException("Initial stability parameters 0..3 must be non-decreasing.", nameof(values));
        }

        if (copy[26] < copy[25])
        {
            throw new ArgumentException("Parameter 26 must be greater than or equal to parameter 25.", nameof(values));
        }

        return new Fsrs7Parameters(copy);
    }

    public double[] ToArray() => (double[])_values.Clone();
}
