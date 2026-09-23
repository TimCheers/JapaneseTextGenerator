using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Scheduling;

/// <summary>
/// Dependency-free C# runtime port of the finished 34-parameter, dual-trace FSRS-7 model.
/// It supports fractional-day intervals and same-day scheduling.
/// Single-precision floats and fused multiply-add mirror the Rust f32 runtime of fsrs-rs.
/// </summary>
/// <remarks>
/// Ported from open-spaced-repetition/fsrs-rs (<c>src/model_v7.rs</c>,
/// <c>src/inference_v7.rs</c>, BSD-3-Clause) at commit
/// <c>c137ee6e096f9217632397a8fb2bdb6f6e1b92ae</c>. See
/// <c>backend/THIRD_PARTY_NOTICES.md</c> and retain that notice on redistribution.
/// </remarks>
public sealed class Fsrs7Scheduler : IFsrsScheduler
{
    private const float StabilityMin = 0.0001f;
    private const float StabilityMax = 36_500.0f;
    private const float DifficultyMin = 1.0f;
    private const float DifficultyMax = 10.0f;
    private const float DesiredRetentionMin = 0.0001f;
    private const float DesiredRetentionMax = 0.9999f;
    private const float MinimumTimeDays = 1.0f / 86_400.0f;
    private const int NewtonIterations = 7;
    private const int BisectionIterations = 50;

    private readonly Fsrs7Parameters _w;
    private readonly float _defaultDesiredRetention;

    public string AlgorithmId => "fsrs-7-dual-trace-34";

    public Fsrs7Scheduler(Fsrs7Options? options = null)
    {
        options ??= new Fsrs7Options();
        options.Validate();
        _w = options.Parameters;
        _defaultDesiredRetention = options.DesiredRetention;
    }

    public FsrsScheduleResult Schedule(FsrsScheduleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rating = request.Rating.ToReferenceValue();
        var reviewedAt = request.ReviewedAt.ToUniversalTime();

        Fsrs7MemoryState nextState;
        float elapsedDays;
        float? retrievabilityBefore;
        var previousStateOpt = request.PreviousState;
        var firstReview = previousStateOpt is null;

        if (firstReview)
        {
            if (request.LastReviewedAt is not null)
            {
                throw new ArgumentException("LastReviewedAt must be null when PreviousState is null.", nameof(request));
            }

            elapsedDays = 0.0f;
            retrievabilityBefore = null;
            nextState = InitializeState(rating);
        }
        else
        {
            if (request.LastReviewedAt is null)
            {
                throw new ArgumentException("LastReviewedAt is required when PreviousState is present.", nameof(request));
            }

            var lastReviewedAt = request.LastReviewedAt.Value.ToUniversalTime();
            var elapsed = reviewedAt - lastReviewedAt;
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "ReviewedAt cannot be before LastReviewedAt.");
            }

            elapsedDays = (float)elapsed.TotalDays;
            var previousState = ValidateState(previousStateOpt!.Value);
            retrievabilityBefore = ForgettingCurve(elapsedDays, previousState);
            nextState = NextState(previousState, elapsedDays, rating, retrievabilityBefore.Value);
        }

        var requestedRetention = request.DesiredRetention ?? _defaultDesiredRetention;
        if (!float.IsFinite(requestedRetention))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Desired retention must be finite.");
        }

        var effectiveRetention = Math.Clamp(requestedRetention, DesiredRetentionMin, DesiredRetentionMax);
        var intervalDays = IntervalAtRetention(nextState, effectiveRetention);
        var dueAt = AddDaysSafely(reviewedAt, intervalDays);

        return new FsrsScheduleResult(
            nextState,
            elapsedDays,
            retrievabilityBefore,
            effectiveRetention,
            intervalDays,
            dueAt,
            firstReview);
    }

    public float Retrievability(Fsrs7MemoryState state, TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        return ForgettingCurve((float)elapsed.TotalDays, ValidateState(state));
    }

    public float IntervalAtRetention(Fsrs7MemoryState state, float desiredRetention)
    {
        if (!float.IsFinite(desiredRetention))
        {
            throw new ArgumentOutOfRangeException(nameof(desiredRetention), "Desired retention must be finite.");
        }

        state = ValidateState(state);

        var target = Math.Clamp(desiredRetention, DesiredRetentionMin, DesiredRetentionMax);
        if (target >= DesiredRetentionMax)
        {
            return 0.0f;
        }

        var minimumLogTime = MathF.Log(MinimumTimeDays);
        var maximumLogTime = MathF.Log(StabilityMax);
        var logTime = MathF.Log(MathF.Max(MathF.Max(state.Stability, state.FastStability), MinimumTimeDays));

        for (var i = 0; i < NewtonIterations; i++)
        {
            logTime = Math.Clamp(logTime, minimumLogTime, maximumLogTime);
            var time = Math.Clamp(MathF.Exp(logTime), MinimumTimeDays, StabilityMax);
            var (retrievability, derivative) = ForgettingCurveAndDerivative(time, state);
            var derivativeInLogSpace = MathF.Min(derivative * time, -1e-12f);
            var step = Math.Clamp((retrievability - target) / derivativeInLogSpace, -4.0f, 4.0f);
            logTime -= step;

            if (!float.IsFinite(logTime))
            {
                return IntervalByBisection(state, target);
            }
        }

        var interval = Math.Clamp(MathF.Exp(logTime), 0.0f, StabilityMax);
        var achieved = ForgettingCurve(interval, state);
        if (float.IsFinite(achieved) && MathF.Abs(achieved - target) <= 1e-3f)
        {
            return interval;
        }

        return IntervalByBisection(state, target);
    }

    private Fsrs7MemoryState InitializeState(int rating)
    {
        var slow = Math.Clamp(_w[rating - 1], StabilityMin, StabilityMax);
        var fast = Math.Clamp(0.8f * slow, StabilityMin, StabilityMax);
        var difficulty = Math.Clamp(InitialDifficulty(rating), DifficultyMin, DifficultyMax);
        return ValidateState(new Fsrs7MemoryState(slow, fast, difficulty));
    }

    private Fsrs7MemoryState NextState(
        Fsrs7MemoryState state,
        float elapsedDays,
        int rating,
        float retrievability)
    {
        var slow = StabilityForSet(state.Stability, state.Difficulty, retrievability, rating, 7);
        var fastRecall = FastComponentRecall(elapsedDays, state.FastStability);
        var fast = StabilityForSet(state.FastStability, state.Difficulty, fastRecall, rating, 15);

        if (rating == (int)FsrsRating.Again)
        {
            fast = MathF.Min(fast, 0.8f * slow);
        }

        fast = Math.Clamp(fast, StabilityMin, StabilityMax);
        var difficulty = NextDifficulty(state.Difficulty, rating, retrievability);
        return ValidateState(new Fsrs7MemoryState(slow, fast, difficulty));
    }

    private float StabilityForSet(
        float lastStability,
        float lastDifficulty,
        float retrievability,
        int rating,
        int start)
    {
        var hardPenalty = rating == (int)FsrsRating.Hard ? _w[start + 6] : 1.0f;
        var easyBonus = rating == (int)FsrsRating.Easy ? _w[start + 7] : 1.0f;

        var failedStability =
            _w[start + 3]
            * (MathF.Pow(lastStability + 1.0f, _w[start + 4]) - 1.0f)
            * MathF.Exp((1.0f - retrievability) * _w[start + 5]);

        var postLapseStability = MathF.Min(lastStability, failedStability);
        var increase =
            MathF.Exp(_w[start] - 1.5f)
            * (11.0f - lastDifficulty)
            * MathF.Pow(lastStability, -_w[start + 1])
            * (MathF.Exp((1.0f - retrievability) * _w[start + 2]) - 1.0f)
            * hardPenalty
            * easyBonus
            + 1.0f;

        var successfulStability = MathF.Max(postLapseStability, lastStability * increase);
        var result = rating > (int)FsrsRating.Again ? successfulStability : postLapseStability;
        return ClampFinite(result, StabilityMin, StabilityMax, "stability");
    }

    private float NextDifficulty(float difficulty, int rating, float retention)
    {
        var delta = -_w[6] * (rating - 3.0f);
        if (rating == (int)FsrsRating.Again)
        {
            delta *= retention + 0.1f;
        }

        var damped = delta * (10.0f - difficulty) / 9.0f;
        var next = difficulty + damped;
        next = 0.01f * InitialDifficulty((int)FsrsRating.Easy) + 0.99f * next;
        return ClampFinite(next, DifficultyMin, DifficultyMax, "difficulty");
    }

    private float InitialDifficulty(int rating) =>
        _w[4] - MathF.Exp(_w[5] * (rating - 1.0f)) + 1.0f;

    private float FastComponentRecall(float elapsedDays, float fastStability)
    {
        var time = MathF.Max(elapsedDays, 0.0f);
        var stability = Math.Clamp(fastStability, StabilityMin, StabilityMax);
        var decayMagnitude = Math.Clamp(_w[23] * MathF.Pow(stability, _w[33] - 0.3f), 0.01f, 0.95f);
        var decay = -decayMagnitude;
        var exponent = MathF.Min(MathF.Log(_w[25]) / decay, 60.0f);
        var factor = MathF.Exp(exponent) - 1.0f;
        return MathF.Pow(1.0f + factor * (time / stability), decay);
    }

    private float ForgettingCurve(float elapsedDays, Fsrs7MemoryState state)
    {
        var (retention, _) = ForgettingCurveAndDerivative(elapsedDays, state);
        return retention;
    }

    private (float Retention, float Derivative) ForgettingCurveAndDerivative(
        float elapsedDays,
        Fsrs7MemoryState state)
    {
        var time = MathF.Max(elapsedDays, 0.0f);
        var slow = MathF.Max(state.Stability, StabilityMin);
        var fast = MathF.Max(state.FastStability, StabilityMin);
        var difficulty = Math.Clamp(state.Difficulty, DifficultyMin, DifficultyMax);

        var decay1Magnitude = Math.Clamp(_w[23] * MathF.Pow(fast, _w[33] - 0.3f), 0.01f, 0.95f);
        var decay1 = -decay1Magnitude;
        var factor1 = MathF.Exp(MathF.Min(MathF.Log(_w[25]) / decay1, 60.0f)) - 1.0f;
        var base1 = 1.0f + factor1 * (time / fast);
        var recall1 = MathF.Pow(base1, decay1);
        var derivative1 = decay1 * MathF.Pow(base1, decay1 - 1.0f) * factor1 / fast;

        var decay2 = -Math.Clamp(_w[24], 0.01f, 0.95f);
        var factor2 = MathF.Pow(_w[26], 1.0f / decay2) - 1.0f;
        var difficultyTimeScale = MathF.Exp((difficulty - 5.0f) * (_w[32] - 0.3f));
        var base2 = 1.0f + factor2 * difficultyTimeScale * (time / slow);
        var recall2 = MathF.Pow(base2, decay2);
        var derivative2 = decay2 * MathF.Pow(base2, decay2 - 1.0f) * factor2 * difficultyTimeScale / slow;

        var weight1 = _w[27] * MathF.Pow(fast, -_w[29]);
        var weight2 = _w[28] * MathF.Pow(slow, _w[30]) * MathF.Exp((difficulty - 5.0f) * (_w[31] - 0.5f));
        var weightSum = MathF.Max(weight1 + weight2, 1e-9f);

        var rawRetention = (weight1 * recall1 + weight2 * recall2) / weightSum;
        var rawDerivative = (weight1 * derivative1 + weight2 * derivative2) / weightSum;
        var retention = MathF.FusedMultiplyAdd(rawRetention, 1.0f - 2e-5f, 1e-5f);
        var derivative = rawDerivative * (1.0f - 2e-5f);

        if (!float.IsFinite(retention) || !float.IsFinite(derivative))
        {
            throw new InvalidOperationException("FSRS-7 produced a non-finite forgetting-curve result.");
        }

        return (retention, derivative);
    }

    private float IntervalByBisection(Fsrs7MemoryState state, float target)
    {
        var low = 0.0f;
        var high = MathF.Max(MathF.Max(state.Stability, state.FastStability), 1.0f);

        while (ForgettingCurve(high, state) > target && high < StabilityMax)
        {
            high = MathF.Min(high * 2.0f, StabilityMax);
        }

        for (var i = 0; i < BisectionIterations; i++)
        {
            var middle = (low + high) * 0.5f;
            if (ForgettingCurve(middle, state) > target)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return Math.Clamp((low + high) * 0.5f, 0.0f, StabilityMax);
    }

    private static Fsrs7MemoryState ValidateState(Fsrs7MemoryState state)
    {
        if (!float.IsFinite(state.Stability) || !float.IsFinite(state.FastStability) || !float.IsFinite(state.Difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Memory state values must be finite.");
        }

        if (state.Stability is < StabilityMin or > StabilityMax
            || state.FastStability is < StabilityMin or > StabilityMax
            || state.Difficulty is < DifficultyMin or > DifficultyMax)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Memory state is outside FSRS-7 runtime bounds.");
        }

        return state;
    }

    private static DateTimeOffset AddDaysSafely(DateTimeOffset start, float days)
    {
        var delta = TimeSpan.FromDays(days);
        var maximumDelta = DateTimeOffset.MaxValue - start;
        return delta > maximumDelta ? DateTimeOffset.MaxValue : start + delta;
    }

    private static float ClampFinite(float value, float minimum, float maximum, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new InvalidOperationException($"FSRS-7 produced a non-finite {name}.");
        }

        return Math.Clamp(value, minimum, maximum);
    }
}
