namespace KotobaApi.Srs.Scheduling;

/// <summary>
/// Immutable FSRS-7 parameter vector (34 parameters), single-precision to mirror
/// the Rust f32 runtime of fsrs-rs.
/// Custom vectors are clipped with the reference parameter-clipper semantics
/// (absolute bounds plus the w1..w3 and w26 ordering constraints); wrong counts
/// and non-finite values are rejected explicitly, mirroring FSRS::new.
/// See backend/THIRD_PARTY_NOTICES.md.
/// </summary>
public sealed class Fsrs7Parameters
{
    public const int ParameterCount = 34;

    private const float StabilityMin = 0.0001f;
    private const float InitialStabilityMax = 100.0f;

    private static readonly float[] DefaultParameterValues =
    [
        0.1104f, 2.2395f, 3.9221f, 11.7841f,
        6.1686f, 0.6457f, 3.6807f,
        1.9795f, 0.0f, 1.3826f, 0.7024f, 0.5999f, 0.8146f, 0.6398f, 1.0f,
        1.3207f, 0.6707f, 3.8668f, 0.4416f, 0.0934f, 1.8631f, 0.6162f, 1.0869f,
        0.1567f, 0.0801f, 0.2421f, 0.9464f, 0.1433f, 0.7145f, 0.0f, 0.5667f, 0.3734f, 0.5333f, 0.3048f,
    ];

    // Absolute clip bounds mirror the reference parameter clipper. Index 26 is
    // relative (clipped against the already-clipped index 25); see FromOptimized.
    private static readonly float[] LowerBounds =
    [
        StabilityMin, StabilityMin, StabilityMin, StabilityMin, 1.0f, 0.001f, 0.1f, 0.0f, 0.0f, 0.3f, 0.01f, 0.1f,
        0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.5f, 0.001f, 0.001f, 0.0f, 0.0f, 1.0f, 0.01f, 0.01f, 0.2f,
        0.0f, 0.01f, 0.1f, 0.0f, 0.1f, 0.0f, 0.0f, 0.0f,
    ];

    private static readonly float[] UpperBounds =
    [
        InitialStabilityMax / 2.0f, InitialStabilityMax, InitialStabilityMax, InitialStabilityMax, 10.0f, 4.0f, 4.0f, 4.0f, 1.2f, 3.0f, 1.5f, 1.0f,
        3.5f, 1.0f, 7.0f, 4.0f, 2.0f, 6.0f, 1.5f, 1.0f, 5.0f, 1.0f, 7.0f, 0.25f, 0.95f, 0.85f,
        0.99f, 1.0f, 1.0f, 0.9f, 1.1f, 1.0f, 0.6f, 0.6f,
    ];

    private readonly float[] _values;

    private Fsrs7Parameters(float[] values) => _values = values;

    public static Fsrs7Parameters Default { get; } = new((float[])DefaultParameterValues.Clone());

    public float this[int index] => _values[index];

    public int Count => _values.Length;

    /// <summary>
    /// Builds a custom vector with reference-compatible clipping: finite values
    /// outside the clipper bounds are pulled inside (indices 1..3 cascade from
    /// the already-clipped predecessor, index 26 clips against index 25).
    /// </summary>
    public static Fsrs7Parameters FromOptimized(IEnumerable<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();

        if (copy.Length != ParameterCount)
        {
            throw new ArgumentException($"FSRS-7 requires exactly {ParameterCount} parameters.", nameof(values));
        }

        for (var i = 0; i < copy.Length; i++)
        {
            if (!float.IsFinite(copy[i]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    $"Parameter {i} must be finite, but was {copy[i]}.");
            }
        }

        copy[0] = Math.Clamp(copy[0], LowerBounds[0], UpperBounds[0]);
        for (var i = 1; i <= 3; i++)
        {
            copy[i] = Math.Clamp(copy[i], copy[i - 1], UpperBounds[i]);
        }

        for (var i = 4; i <= 25; i++)
        {
            copy[i] = Math.Clamp(copy[i], LowerBounds[i], UpperBounds[i]);
        }

        copy[26] = Math.Clamp(copy[26], copy[25], UpperBounds[26]);

        for (var i = 27; i < copy.Length; i++)
        {
            copy[i] = Math.Clamp(copy[i], LowerBounds[i], UpperBounds[i]);
        }

        return new Fsrs7Parameters(copy);
    }

    public float[] ToArray() => (float[])_values.Clone();
}
