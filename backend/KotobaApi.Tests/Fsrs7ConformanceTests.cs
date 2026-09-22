using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Scheduling;
using Xunit;

namespace KotobaApi.Tests;

/// <summary>
/// Numerical conformance of <see cref="Fsrs7Scheduler"/> against the FSRS-7 reference
/// implementation (open-spaced-repetition/fsrs-rs, dual-trace 34-parameter model).
/// Reference vectors were generated from the current reference equations; see
/// backend/THIRD_PARTY_NOTICES.md. Behavioral relationships live in
/// <see cref="SrsSchedulingTests"/>.
/// </summary>
public sealed class Fsrs7ConformanceTests
{
    private readonly Fsrs7Scheduler _scheduler = new();

    [Fact]
    public void DefaultParameters_MatchCurrentReferenceShape()
    {
        var parameters = Fsrs7Parameters.Default;
        Assert.Equal(34, parameters.Count);
        AssertClose(0.1104, parameters[0], 1e-12);
        AssertClose(0.3048, parameters[33], 1e-12);
    }

    [Fact]
    public void ForgettingCurve_MatchesReferenceVector()
    {
        var state = new Fsrs7MemoryState(12.0, 9.6, 5.0);
        var actual = _scheduler.Retrievability(state, TimeSpan.FromDays(10));
        AssertClose(0.9196594795573547, actual, 1e-10);
    }

    [Fact]
    public void IntervalSolver_HitsRequestedRetention()
    {
        var state = new Fsrs7MemoryState(10.0, 8.0, 5.0);
        var interval = _scheduler.IntervalAtRetention(state, 0.90);
        AssertClose(12.750801944882769, interval, 1e-8);

        var achieved = _scheduler.Retrievability(state, TimeSpan.FromDays(interval));
        AssertClose(0.90, achieved, 1e-10);
    }

    [Fact]
    public void IntervalSolver_HigherRetention_ShorterInterval()
    {
        var state = new Fsrs7MemoryState(10.0, 8.0, 5.0);

        var interval = _scheduler.IntervalAtRetention(state, 0.95);

        AssertClose(2.0461024623163544, interval, 1e-8);
        var achieved = _scheduler.Retrievability(state, TimeSpan.FromDays(interval));
        AssertClose(0.95, achieved, 1e-10);
    }

    [Fact]
    public void MixedSameDaySequence_MatchesReferenceImplementation()
    {
        var t0 = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);
        Fsrs7MemoryState? state = null;
        DateTimeOffset? last = null;

        var r1 = Schedule(ref state, ref last, t0, FsrsRating.Good);
        AssertState(r1.State, 3.9221, 3.13768, 3.530723981276);
        AssertClose(4.777721806897, r1.IntervalDays, 1e-8);

        var r2 = Schedule(ref state, ref last, t0.AddHours(6), FsrsRating.Good);
        AssertClose(0.9511589577002326, r2.RetrievabilityBeforeReview!.Value, 1e-10);
        AssertState(r2.State, 7.227840824218, 101.86814470363, 3.497716743203);
        AssertClose(14.995515647119, r2.IntervalDays, 1e-8);

        var r3At = t0.AddHours(6).AddDays(1.5);
        var r3 = Schedule(ref state, ref last, r3At, FsrsRating.Hard);
        AssertState(r3.State, 10.180041917093, 218.949232240597, 6.097664515672);
        AssertClose(11.329166709527, r3.IntervalDays, 1e-8);

        var r4At = r3At.AddHours(2.4);
        var r4 = Schedule(ref state, ref last, r4At, FsrsRating.Again);
        AssertState(r4.State, 2.310638780819, 0.421959284403, 9.474576116067);
        AssertClose(0.008041909896, r4.IntervalDays, 1e-9);

        var r5 = Schedule(ref state, ref last, r4At.AddDays(2), FsrsRating.Easy);
        AssertState(r5.State, 4.367908752125, 23.296717443267, 9.169398310789);
        AssertClose(0.74170650089, r5.IntervalDays, 1e-8);
    }

    [Fact]
    public void PreviousStateRequiresLastReviewTimestamp()
    {
        var request = new FsrsScheduleRequest(
            new Fsrs7MemoryState(1, 0.8, 5),
            null,
            DateTimeOffset.UtcNow,
            FsrsRating.Good);

        Assert.Throws<ArgumentException>(() => _scheduler.Schedule(request));
    }

    // Regression anchors below: captured from this implementation, not independently
    // derived from fsrs-rs. They guard against accidental formula drift; they do not
    // by themselves prove conformance.

    [Theory]
    [InlineData(FsrsRating.Again, 0.1104, 0.08832, 6.1686, 3.827564746804539E-05)]
    [InlineData(FsrsRating.Hard, 2.2395, 1.7916, 5.261278312731786, 0.5974611730736041)]
    [InlineData(FsrsRating.Good, 3.9221, 3.13768, 3.5307239812763354, 4.7777218068967535)]
    [InlineData(FsrsRating.Easy, 11.7841, 9.42728, 1.0, 53.86951316227517)]
    public void FirstReview_InitialState_Anchored(
        FsrsRating rating, double stability, double fastStability, double difficulty, double intervalDays)
    {
        var t0 = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

        var result = _scheduler.Schedule(new FsrsScheduleRequest(null, null, t0, rating));

        Assert.True(result.WasFirstReview);
        AssertState(result.State, stability, fastStability, difficulty);
        AssertClose(intervalDays, result.IntervalDays, 1e-8);
    }

    [Fact]
    public void SuccessAfterLongDelay_Anchored()
    {
        var t0 = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);
        var first = _scheduler.Schedule(new FsrsScheduleRequest(null, null, t0, FsrsRating.Good));

        var late = _scheduler.Schedule(new FsrsScheduleRequest(first.State, t0, t0.AddDays(30), FsrsRating.Good));

        AssertClose(0.8127792098533597, late.RetrievabilityBeforeReview!.Value, 1e-10);
        AssertState(late.State, 17.902277510538475, 219.67286873457817, 3.4977167432025262);
        AssertClose(47.952956594013294, late.IntervalDays, 1e-8);
    }

    [Fact]
    public void SameState_HigherRetention_ShorterScheduleInterval()
    {
        var t0 = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);
        var first = _scheduler.Schedule(new FsrsScheduleRequest(null, null, t0, FsrsRating.Good));
        var at = t0.AddDays(30);

        var at90 = _scheduler.Schedule(new FsrsScheduleRequest(first.State, t0, at, FsrsRating.Good, 0.90));
        var at95 = _scheduler.Schedule(new FsrsScheduleRequest(first.State, t0, at, FsrsRating.Good, 0.95));

        Assert.Equal(at90.State, at95.State);
        AssertClose(47.952956594013294, at90.IntervalDays, 1e-8);
        AssertClose(12.695151663638736, at95.IntervalDays, 1e-8);
        Assert.True(at95.DueAt < at90.DueAt);
    }

    private FsrsScheduleResult Schedule(
        ref Fsrs7MemoryState? state,
        ref DateTimeOffset? last,
        DateTimeOffset reviewedAt,
        FsrsRating rating,
        double? desiredRetention = null)
    {
        var result = _scheduler.Schedule(new FsrsScheduleRequest(state, last, reviewedAt, rating, desiredRetention));
        state = result.State;
        last = reviewedAt;
        return result;
    }

    private static void AssertState(Fsrs7MemoryState actual, double slow, double fast, double difficulty)
    {
        AssertClose(slow, actual.Stability, 1e-8);
        AssertClose(fast, actual.FastStability, 1e-8);
        AssertClose(difficulty, actual.Difficulty, 1e-8);
    }

    private static void AssertClose(double expected, double actual, double tolerance) =>
        Assert.InRange(Math.Abs(expected - actual), 0.0, tolerance);
}
