using KotobaApi.Srs.Domain;

namespace KotobaApi.Srs.Scheduling;

/// <summary>
/// Dependency-free C# runtime port of the finished 34-parameter, dual-trace FSRS-7 model.
/// It supports fractional-day intervals and same-day scheduling.
/// </summary>
public sealed class Fsrs7Scheduler : IFsrsScheduler
{
    private const double StabilityMin = 0.0001;
    private const double StabilityMax = 36_500.0;
    private const double DifficultyMin = 1.0;
    private const double DifficultyMax = 10.0;
    private const double DesiredRetentionMin = 0.0001;
    private const double DesiredRetentionMax = 0.9999;
    private const double MinimumTimeDays = 1.0 / 86_400.0;
    private const int NewtonIterations = 7;
    private const int BisectionIterations = 50;

    private readonly Fsrs7Parameters _w;
    private readonly double _defaultDesiredRetention;

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
        double elapsedDays;
        double? retrievabilityBefore;
        var previousStateOpt = request.PreviousState;
        var firstReview = previousStateOpt is null;

        if (firstReview)
        {
            if (request.LastReviewedAt is not null)
            {
                throw new ArgumentException("LastReviewedAt must be null when PreviousState is null.", nameof(request));
            }

            elapsedDays = 0.0;
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

            elapsedDays = elapsed.TotalDays;
            var previousState = ValidateState(previousStateOpt!.Value);
            retrievabilityBefore = ForgettingCurve(elapsedDays, previousState);
            nextState = NextState(previousState, elapsedDays, rating, retrievabilityBefore.Value);
        }

        var desiredRetention = request.DesiredRetention ?? _defaultDesiredRetention;
        ValidateDesiredRetention(desiredRetention);
        var intervalDays = IntervalAtRetention(nextState, desiredRetention);
        var dueAt = AddDaysSafely(reviewedAt, intervalDays);

        return new FsrsScheduleResult(
            nextState,
            elapsedDays,
            retrievabilityBefore,
            desiredRetention,
            intervalDays,
            dueAt,
            firstReview);
    }

    public double Retrievability(Fsrs7MemoryState state, TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        return ForgettingCurve(elapsed.TotalDays, ValidateState(state));
    }

    public double IntervalAtRetention(Fsrs7MemoryState state, double desiredRetention)
    {
        ValidateDesiredRetention(desiredRetention);
        state = ValidateState(state);

        var target = Clamp(desiredRetention, DesiredRetentionMin, DesiredRetentionMax);
        if (target >= DesiredRetentionMax)
        {
            return 0.0;
        }

        var minimumLogTime = Math.Log(MinimumTimeDays);
        var maximumLogTime = Math.Log(StabilityMax);
        var logTime = Math.Log(Math.Max(Math.Max(state.Stability, state.FastStability), MinimumTimeDays));

        for (var i = 0; i < NewtonIterations; i++)
        {
            logTime = Clamp(logTime, minimumLogTime, maximumLogTime);
            var time = Clamp(Math.Exp(logTime), MinimumTimeDays, StabilityMax);
            var (retrievability, derivative) = ForgettingCurveAndDerivative(time, state);
            var derivativeInLogSpace = Math.Min(derivative * time, -1e-12);
            var step = Clamp((retrievability - target) / derivativeInLogSpace, -4.0, 4.0);
            logTime -= step;

            if (!double.IsFinite(logTime))
            {
                return IntervalByBisection(state, target);
            }
        }

        var interval = Clamp(Math.Exp(logTime), 0.0, StabilityMax);
        var achieved = ForgettingCurve(interval, state);
        if (double.IsFinite(achieved) && Math.Abs(achieved - target) <= 1e-3)
        {
            return interval;
        }

        return IntervalByBisection(state, target);
    }

    private Fsrs7MemoryState InitializeState(int rating)
    {
        var slow = Clamp(_w[rating - 1], StabilityMin, StabilityMax);
        var fast = Clamp(0.8 * slow, StabilityMin, StabilityMax);
        var difficulty = Clamp(InitialDifficulty(rating), DifficultyMin, DifficultyMax);
        return ValidateState(new Fsrs7MemoryState(slow, fast, difficulty));
    }

    private Fsrs7MemoryState NextState(
        Fsrs7MemoryState state,
        double elapsedDays,
        int rating,
        double retrievability)
    {
        var slow = StabilityForSet(state.Stability, state.Difficulty, retrievability, rating, 7);
        var fastRecall = FastComponentRecall(elapsedDays, state.FastStability);
        var fast = StabilityForSet(state.FastStability, state.Difficulty, fastRecall, rating, 15);

        if (rating == (int)FsrsRating.Again)
        {
            fast = Math.Min(fast, 0.8 * slow);
        }

        fast = Clamp(fast, StabilityMin, StabilityMax);
        var difficulty = NextDifficulty(state.Difficulty, rating, retrievability);
        return ValidateState(new Fsrs7MemoryState(slow, fast, difficulty));
    }

    private double StabilityForSet(
        double lastStability,
        double lastDifficulty,
        double retrievability,
        int rating,
        int start)
    {
        var hardPenalty = rating == (int)FsrsRating.Hard ? _w[start + 6] : 1.0;
        var easyBonus = rating == (int)FsrsRating.Easy ? _w[start + 7] : 1.0;

        var failedStability =
            _w[start + 3]
            * (Math.Pow(lastStability + 1.0, _w[start + 4]) - 1.0)
            * Math.Exp((1.0 - retrievability) * _w[start + 5]);

        var postLapseStability = Math.Min(lastStability, failedStability);
        var increase =
            Math.Exp(_w[start] - 1.5)
            * (11.0 - lastDifficulty)
            * Math.Pow(lastStability, -_w[start + 1])
            * (Math.Exp((1.0 - retrievability) * _w[start + 2]) - 1.0)
            * hardPenalty
            * easyBonus
            + 1.0;

        var successfulStability = Math.Max(postLapseStability, lastStability * increase);
        var result = rating > (int)FsrsRating.Again ? successfulStability : postLapseStability;
        return ClampFinite(result, StabilityMin, StabilityMax, "stability");
    }

    private double NextDifficulty(double difficulty, int rating, double retention)
    {
        var delta = -_w[6] * (rating - 3.0);
        if (rating == (int)FsrsRating.Again)
        {
            delta *= retention + 0.1;
        }

        var damped = delta * (10.0 - difficulty) / 9.0;
        var next = difficulty + damped;
        next = 0.01 * InitialDifficulty((int)FsrsRating.Easy) + 0.99 * next;
        return ClampFinite(next, DifficultyMin, DifficultyMax, "difficulty");
    }

    private double InitialDifficulty(int rating) =>
        _w[4] - Math.Exp(_w[5] * (rating - 1.0)) + 1.0;

    private double FastComponentRecall(double elapsedDays, double fastStability)
    {
        var time = Math.Max(elapsedDays, 0.0);
        var stability = Clamp(fastStability, StabilityMin, StabilityMax);
        var decayMagnitude = Clamp(_w[23] * Math.Pow(stability, _w[33] - 0.3), 0.01, 0.95);
        var decay = -decayMagnitude;
        var exponent = Math.Min(Math.Log(_w[25]) / decay, 60.0);
        var factor = Math.Exp(exponent) - 1.0;
        return Math.Pow(1.0 + factor * (time / stability), decay);
    }

    private double ForgettingCurve(double elapsedDays, Fsrs7MemoryState state)
    {
        var (retention, _) = ForgettingCurveAndDerivative(elapsedDays, state);
        return retention;
    }

    private (double Retention, double Derivative) ForgettingCurveAndDerivative(
        double elapsedDays,
        Fsrs7MemoryState state)
    {
        var time = Math.Max(elapsedDays, 0.0);
        var slow = Math.Max(state.Stability, StabilityMin);
        var fast = Math.Max(state.FastStability, StabilityMin);
        var difficulty = Clamp(state.Difficulty, DifficultyMin, DifficultyMax);

        var decay1Magnitude = Clamp(_w[23] * Math.Pow(fast, _w[33] - 0.3), 0.01, 0.95);
        var decay1 = -decay1Magnitude;
        var factor1 = Math.Exp(Math.Min(Math.Log(_w[25]) / decay1, 60.0)) - 1.0;
        var base1 = 1.0 + factor1 * (time / fast);
        var recall1 = Math.Pow(base1, decay1);
        var derivative1 = decay1 * Math.Pow(base1, decay1 - 1.0) * factor1 / fast;

        var decay2 = -Clamp(_w[24], 0.01, 0.95);
        var factor2 = Math.Pow(_w[26], 1.0 / decay2) - 1.0;
        var difficultyTimeScale = Math.Exp((difficulty - 5.0) * (_w[32] - 0.3));
        var base2 = 1.0 + factor2 * difficultyTimeScale * (time / slow);
        var recall2 = Math.Pow(base2, decay2);
        var derivative2 = decay2 * Math.Pow(base2, decay2 - 1.0) * factor2 * difficultyTimeScale / slow;

        var weight1 = _w[27] * Math.Pow(fast, -_w[29]);
        var weight2 = _w[28] * Math.Pow(slow, _w[30]) * Math.Exp((difficulty - 5.0) * (_w[31] - 0.5));
        var weightSum = Math.Max(weight1 + weight2, 1e-9);

        var rawRetention = (weight1 * recall1 + weight2 * recall2) / weightSum;
        var rawDerivative = (weight1 * derivative1 + weight2 * derivative2) / weightSum;
        var retention = rawRetention * (1.0 - 2e-5) + 1e-5;
        var derivative = rawDerivative * (1.0 - 2e-5);

        if (!double.IsFinite(retention) || !double.IsFinite(derivative))
        {
            throw new InvalidOperationException("FSRS-7 produced a non-finite forgetting-curve result.");
        }

        return (retention, derivative);
    }

    private double IntervalByBisection(Fsrs7MemoryState state, double target)
    {
        var low = 0.0;
        var high = Math.Max(Math.Max(state.Stability, state.FastStability), 1.0);

        while (ForgettingCurve(high, state) > target && high < StabilityMax)
        {
            high = Math.Min(high * 2.0, StabilityMax);
        }

        for (var i = 0; i < BisectionIterations; i++)
        {
            var middle = (low + high) * 0.5;
            if (ForgettingCurve(middle, state) > target)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return Clamp((low + high) * 0.5, 0.0, StabilityMax);
    }

    private static Fsrs7MemoryState ValidateState(Fsrs7MemoryState state)
    {
        if (!double.IsFinite(state.Stability) || !double.IsFinite(state.FastStability) || !double.IsFinite(state.Difficulty))
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

    private static void ValidateDesiredRetention(double desiredRetention)
    {
        if (!double.IsFinite(desiredRetention) || desiredRetention is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredRetention), "Desired retention must be between 0 and 1.");
        }
    }

    private static DateTimeOffset AddDaysSafely(DateTimeOffset start, double days)
    {
        var delta = TimeSpan.FromDays(days);
        var maximumDelta = DateTimeOffset.MaxValue - start;
        return delta > maximumDelta ? DateTimeOffset.MaxValue : start + delta;
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);

    private static double ClampFinite(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"FSRS-7 produced a non-finite {name}.");
        }

        return Clamp(value, minimum, maximum);
    }
}
