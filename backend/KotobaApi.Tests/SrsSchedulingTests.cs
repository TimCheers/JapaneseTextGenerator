using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Learning;
using KotobaApi.Srs.Scheduling;
using Xunit;

namespace KotobaApi.Tests;

/// <summary>
/// Scheduling cases S1-S21 from docs/tests.md.
/// Asserts required relationships, not invented day counts.
/// Fixed clock: now = 2026-09-23 00:00 UTC, desired retention 0.90 (defaults).
/// </summary>
public sealed class SrsSchedulingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    private static InMemoryLearningEngine NewEngine() => new();

    private static Guid NewId() => Guid.NewGuid();

    // S1. Completely new word + Again.
    [Fact]
    public void S1_NewWord_Again_CreatesStateDueInFuture()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();

        var result = engine.RecordNewWordAssessment(NewId(), user, word, Now, FsrsRating.Again);

        Assert.True(result.Event.WasFirstReview);
        Assert.Equal(1, result.ProgressAfter.ReviewCount);
        Assert.Equal(Now, result.ProgressAfter.LastReviewedAt);
        Assert.NotNull(result.ProgressAfter.MemoryState);
        Assert.True(result.ProgressAfter.DueAt > Now);
        // A first Again is not a lapse: there was no prior success to lose.
        Assert.Equal(0, result.ProgressAfter.LapseCount);
    }

    // S1-S4. Initial intervals order Again < Hard < Good < Easy.
    [Fact]
    public void S1_S4_NewWord_InitialIntervals_OrderByRating()
    {
        var engine = NewEngine();
        var user = NewId();

        var again = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Again);
        var hard = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Hard);
        var good = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Good);
        var easy = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Easy);

        Assert.True(again.ProgressAfter.DueAt < hard.ProgressAfter.DueAt);
        Assert.True(hard.ProgressAfter.DueAt < good.ProgressAfter.DueAt);
        Assert.True(good.ProgressAfter.DueAt < easy.ProgressAfter.DueAt);
    }

    // S2. New word + Hard creates state.
    [Fact]
    public void S2_NewWord_Hard_CreatesState()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();

        var result = engine.RecordNewWordAssessment(NewId(), user, word, Now, FsrsRating.Hard);

        Assert.True(result.Event.WasFirstReview);
        Assert.Equal(1, result.ProgressAfter.ReviewCount);
        Assert.Equal(Now, result.ProgressAfter.LastReviewedAt);
    }

    // S3. New word + Good creates full memory state.
    [Fact]
    public void S3_NewWord_Good_CreatesFullState()
    {
        var engine = NewEngine();

        var result = engine.RecordNewWordAssessment(NewId(), NewId(), NewId(), Now, FsrsRating.Good);

        var state = Assert.NotNull(result.ProgressAfter.MemoryState);
        Assert.InRange(state.Stability, 0.0001, 36500.0);
        Assert.InRange(state.FastStability, 0.0001, 36500.0);
        Assert.InRange(state.Difficulty, 1.0, 10.0);
        Assert.NotNull(result.ProgressAfter.DueAt);
    }

    // S4. New word + Easy gives the longest initial interval (covered by ordering test).
    [Fact]
    public void S4_NewWord_Easy_LongestInitialInterval()
    {
        var engine = NewEngine();
        var user = NewId();

        var good = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Good);
        var easy = engine.RecordNewWordAssessment(NewId(), user, NewId(), Now, FsrsRating.Easy);

        Assert.True(easy.ProgressAfter.DueAt > good.ProgressAfter.DueAt);
    }

    // S5. Successful Good review on a due word.
    [Fact]
    public void S5_DueWord_Good_IncreasesStabilityAndPushesDueDate()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var firstAt = Now.AddDays(-6); // first Good interval is ~4.78d, so this is due.

        var first = engine.ApplyReview(new ReviewCommand(NewId(), user, word, firstAt, FsrsRating.Good, ReviewSource.ExplicitRating));
        Assert.True(first.ProgressAfter.DueAt <= Now);

        var second = engine.ApplyReview(new ReviewCommand(NewId(), user, word, Now, FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.Equal(2, second.ProgressAfter.ReviewCount);
        Assert.Equal(0, second.ProgressAfter.LapseCount);
        Assert.Equal(Now, second.ProgressAfter.LastReviewedAt);
        Assert.True(second.ProgressAfter.DueAt > Now);
        Assert.True(second.Event.StateAfterReview.Stability > first.Event.StateAfterReview.Stability);
    }

    // S6. Easy produces a later review than Good from identical state.
    [Fact]
    public void S6_SameState_Easy_LaterThanGood()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var good = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good));
        var easy = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Easy));

        Assert.True(easy.DueAt > good.DueAt);
    }

    // S7. Hard produces an earlier review than Good from identical state.
    [Fact]
    public void S7_SameState_Hard_EarlierThanGood()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var hard = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Hard));
        var good = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good));

        Assert.True(hard.DueAt < good.DueAt);
    }

    // S8. Again after knowing a word: lapse counted, due much earlier, history kept.
    [Fact]
    public void S8_KnownWord_Again_CountsLapseAndShortensDue()
    {
        var engine = NewEngine();
        var user = NewId();
        var firstAt = Now.AddDays(-6);

        var lapsedWord = NewId();
        engine.ApplyReview(new ReviewCommand(NewId(), user, lapsedWord, firstAt, FsrsRating.Good, ReviewSource.ExplicitRating));
        var lapsed = engine.ApplyReview(new ReviewCommand(NewId(), user, lapsedWord, Now, FsrsRating.Again, ReviewSource.ExplicitRating));

        var goodWord = NewId();
        engine.ApplyReview(new ReviewCommand(NewId(), user, goodWord, firstAt, FsrsRating.Good, ReviewSource.ExplicitRating));
        var good = engine.ApplyReview(new ReviewCommand(NewId(), user, goodWord, Now, FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.Equal(2, lapsed.ProgressAfter.ReviewCount);
        Assert.Equal(1, lapsed.ProgressAfter.LapseCount);
        Assert.False(lapsed.Event.WasFirstReview);
        Assert.True(lapsed.ProgressAfter.DueAt < good.ProgressAfter.DueAt);
    }

    // S9. A lapse can cause a same-day review (fractional intervals, no day rounding).
    [Fact]
    public void S9_LapseSequence_CanProduceSubHourInterval()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var t0 = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

        engine.ApplyReview(new ReviewCommand(NewId(), user, word, t0, FsrsRating.Good, ReviewSource.ExplicitRating));
        engine.ApplyReview(new ReviewCommand(NewId(), user, word, t0.AddHours(6), FsrsRating.Good, ReviewSource.ExplicitRating));
        var hardAt = t0.AddHours(6).AddDays(1.5);
        engine.ApplyReview(new ReviewCommand(NewId(), user, word, hardAt, FsrsRating.Hard, ReviewSource.ExplicitRating));
        var lapseAt = hardAt.AddHours(2.4);
        var lapsed = engine.ApplyReview(new ReviewCommand(NewId(), user, word, lapseAt, FsrsRating.Again, ReviewSource.ExplicitRating));

        var interval = lapsed.ProgressAfter.DueAt!.Value - lapsed.Event.ReviewedAt;
        Assert.True(interval > TimeSpan.Zero);
        Assert.True(interval < TimeSpan.FromHours(1));
    }

    // S10. Same-day second review uses exact elapsed time (6h = 0.25d).
    [Fact]
    public void S10_SameDaySecondReview_UsesFractionalElapsedDays()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var morning = new DateTimeOffset(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        engine.ApplyReview(new ReviewCommand(NewId(), user, word, morning, FsrsRating.Good, ReviewSource.ExplicitRating));
        var second = engine.ApplyReview(new ReviewCommand(NewId(), user, word, morning.AddHours(6), FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.InRange(Math.Abs(second.Event.ElapsedDays - 0.25), 0.0, 1e-9);
    }

    // S11. Good after a short interval still grows stability.
    [Fact]
    public void S11_GoodAfterShortInterval_StabilityGrows()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var afterShortGap = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, firstAt.AddHours(6), FsrsRating.Good));

        Assert.True(afterShortGap.State.Stability > prior.State.Stability);
    }

    // S12. Good after a long difficult interval grows stability more than after a short gap.
    [Fact]
    public void S12_GoodAfterLongInterval_GrowsMoreThanShortGap()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-60);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var shortGap = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, firstAt.AddHours(6), FsrsRating.Good));
        var longGap = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, firstAt.AddDays(30), FsrsRating.Good));

        Assert.True(longGap.State.Stability > shortGap.State.Stability);
    }

    // S13. Repeated Again makes a word harder, within bounds.
    [Fact]
    public void S13_RepeatedAgain_DifficultyRisesWithinBounds()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var at = Now.AddHours(-3);

        var first = engine.ApplyReview(new ReviewCommand(NewId(), user, word, at, FsrsRating.Again, ReviewSource.ExplicitRating));
        engine.ApplyReview(new ReviewCommand(NewId(), user, word, at.AddHours(1), FsrsRating.Again, ReviewSource.ExplicitRating));
        var third = engine.ApplyReview(new ReviewCommand(NewId(), user, word, at.AddHours(2), FsrsRating.Again, ReviewSource.ExplicitRating));

        Assert.True(third.Event.StateAfterReview.Difficulty > first.Event.StateAfterReview.Difficulty);
        foreach (var e in engine.GetHistory(user, word))
        {
            Assert.InRange(e.StateAfterReview.Difficulty, 1.0, 10.0);
        }
    }

    // S14. Easy reviews from a hard state reduce difficulty, never below 1.
    [Fact]
    public void S14_EasyFromHardState_DifficultyFallsWithFloor()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var at = Now.AddDays(-4);

        var hard = engine.ApplyReview(new ReviewCommand(NewId(), user, word, at, FsrsRating.Again, ReviewSource.ExplicitRating));
        var hardDifficulty = hard.Event.StateAfterReview.Difficulty;

        AppliedReview? last = null;
        for (var i = 1; i <= 3; i++)
        {
            last = engine.ApplyReview(new ReviewCommand(NewId(), user, word, at.AddDays(i), FsrsRating.Easy, ReviewSource.ExplicitRating));
            Assert.True(last.Event.StateAfterReview.Difficulty >= 1.0);
        }

        Assert.True(last!.Event.StateAfterReview.Difficulty < hardDifficulty);
    }

    // S15. Higher desired retention means earlier review.
    [Fact]
    public void S15_HigherRetention_EarlierDue()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var at90 = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good, 0.90));
        var at95 = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good, 0.95));

        Assert.True(at95.DueAt < at90.DueAt);
    }

    // S16. Lower desired retention means later review.
    [Fact]
    public void S16_LowerRetention_LaterDue()
    {
        var scheduler = new Fsrs7Scheduler();
        var firstAt = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, firstAt, FsrsRating.Good));

        var at85 = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good, 0.85));
        var at90 = scheduler.Schedule(new FsrsScheduleRequest(prior.State, firstAt, Now, FsrsRating.Good, 0.90));

        Assert.True(at85.DueAt > at90.DueAt);
    }

    // S17. Equivalent timestamps in different timezones give identical results.
    [Fact]
    public void S17_SameInstantDifferentTimezone_IdenticalResult()
    {
        var engine = NewEngine();
        var user = NewId();
        var utc = Now;
        var jst = new DateTimeOffset(2026, 9, 23, 9, 0, 0, TimeSpan.FromHours(9));

        var a = engine.RecordNewWordAssessment(NewId(), user, NewId(), utc, FsrsRating.Good);
        var b = engine.RecordNewWordAssessment(NewId(), user, NewId(), jst, FsrsRating.Good);

        Assert.Equal(a.Event.StateAfterReview, b.Event.StateAfterReview);
        Assert.Equal(a.ProgressAfter.DueAt, b.ProgressAfter.DueAt);
    }

    // S18. Review earlier than previous review is rejected.
    [Fact]
    public void S18_ReviewBeforeLastReviewed_Rejected()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();

        engine.ApplyReview(new ReviewCommand(
            NewId(), user, word,
            new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero),
            FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.ApplyReview(new ReviewCommand(
                NewId(), user, word,
                new DateTimeOffset(2026, 9, 23, 9, 0, 0, TimeSpan.Zero),
                FsrsRating.Good, ReviewSource.ExplicitRating)));
    }

    // S19. Exact same-time review has zero elapsed and stays valid.
    [Fact]
    public void S19_SameTimestampReview_ZeroElapsedValid()
    {
        var scheduler = new Fsrs7Scheduler();
        var at = Now.AddDays(-6);
        var prior = scheduler.Schedule(new FsrsScheduleRequest(null, null, at, FsrsRating.Good));

        var repeat = scheduler.Schedule(new FsrsScheduleRequest(prior.State, Now, Now, FsrsRating.Good));

        Assert.Equal(0.0, repeat.ElapsedDays);
        Assert.True(repeat.DueAt >= Now);
    }

    // S20. Very overdue word still schedules without NaN/overflow.
    [Fact]
    public void S20_VeryOverdueWord_SchedulesFiniteResult()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();

        engine.ApplyReview(new ReviewCommand(NewId(), user, word, new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), FsrsRating.Good, ReviewSource.ExplicitRating));
        var result = engine.ApplyReview(new ReviewCommand(NewId(), user, word, Now, FsrsRating.Good, ReviewSource.ExplicitRating));

        var state = result.Event.StateAfterReview;
        Assert.True(double.IsFinite(state.Stability));
        Assert.True(double.IsFinite(state.FastStability));
        Assert.True(double.IsFinite(state.Difficulty));
        Assert.True(double.IsFinite(result.Event.ScheduledIntervalDays));
        Assert.True(result.ProgressAfter.DueAt > Now);
    }

    // S21. Stability stays within bounds across an extreme sequence.
    [Fact]
    public void S21_ExtremeSequence_StabilityWithinBounds()
    {
        var engine = NewEngine();
        var user = NewId();
        var word = NewId();
        var ratings = new[] { FsrsRating.Good, FsrsRating.Again, FsrsRating.Again, FsrsRating.Easy, FsrsRating.Again, FsrsRating.Good, FsrsRating.Easy, FsrsRating.Hard };
        var at = Now.AddDays(-ratings.Length);

        foreach (var rating in ratings)
        {
            var result = engine.ApplyReview(new ReviewCommand(NewId(), user, word, at, rating, ReviewSource.ExplicitRating));
            var state = result.Event.StateAfterReview;
            Assert.InRange(state.Stability, 0.0001, 36500.0);
            Assert.InRange(state.FastStability, 0.0001, 36500.0);
            Assert.InRange(state.Difficulty, 1.0, 10.0);
            at = at.AddDays(1);
        }
    }
}
