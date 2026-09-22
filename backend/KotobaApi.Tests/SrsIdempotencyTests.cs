using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Learning;
using KotobaApi.Srs.Reading;
using Xunit;

namespace KotobaApi.Tests;

/// <summary>
/// Idempotency cases I1-I3 from docs/tests.md, plus the reading-gate behavior
/// the engine depends on (engaged reading = Good, reveal = Hard, ignored gates change nothing).
/// </summary>
public sealed class SrsIdempotencyTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);

    // I1. Identical retry returns the same result; counts move once.
    [Fact]
    public void I1_IdenticalRetry_SameResultSingleCount()
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var word = Guid.NewGuid();
        var command = new ReviewCommand(Guid.NewGuid(), user, word, At, FsrsRating.Good, ReviewSource.ExplicitRating);

        var first = engine.ApplyReview(command);
        var retry = engine.ApplyReview(command);

        Assert.Equal(first, retry);
        Assert.Equal(1, engine.GetProgress(user, word)!.ReviewCount);
        Assert.Single(engine.GetHistory(user, word));
        Assert.Equal(first.ProgressAfter.DueAt, retry.ProgressAfter.DueAt);
    }

    // I2. Same EventId with a different rating is rejected.
    [Fact]
    public void I2_SameEventIdDifferentRating_Rejected()
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var word = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        engine.ApplyReview(new ReviewCommand(eventId, user, word, At, FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.Throws<InvalidOperationException>(() =>
            engine.ApplyReview(new ReviewCommand(eventId, user, word, At, FsrsRating.Again, ReviewSource.ExplicitRating)));
    }

    // I3. Same EventId with a different word is rejected.
    [Fact]
    public void I3_SameEventIdDifferentWord_Rejected()
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        engine.ApplyReview(new ReviewCommand(eventId, user, Guid.NewGuid(), At, FsrsRating.Good, ReviewSource.ExplicitRating));

        Assert.Throws<InvalidOperationException>(() =>
            engine.ApplyReview(new ReviewCommand(eventId, user, Guid.NewGuid(), At, FsrsRating.Good, ReviewSource.ExplicitRating)));
    }

    [Fact]
    public void Reading_EngagedReading_AppliesGoodWithSource()
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var word = Guid.NewGuid();

        var result = engine.RecordContextualReading(new ContextualReadingObservation(
            Guid.NewGuid(), user, word, At, true, TimeSpan.FromSeconds(3), true));

        Assert.Equal(ContextualReadingDisposition.Applied, result.Disposition);
        Assert.NotNull(result.AppliedReview);
        Assert.Equal(FsrsRating.Good, result.AppliedReview.Event.Rating);
        Assert.Equal(ReviewSource.ContextualReading, result.AppliedReview.Event.Source);
    }

    [Fact]
    public void Reading_MeaningReveal_AppliesHardWithSource()
    {
        var engine = new InMemoryLearningEngine();

        var result = engine.RecordMeaningReveal(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), At);

        Assert.Equal(FsrsRating.Hard, result.Event.Rating);
        Assert.Equal(ReviewSource.MeaningRevealed, result.Event.Source);
    }

    [Theory]
    [InlineData(false, 3000, true, ContextualReadingDisposition.IgnoredTargetNotVisible)]
    [InlineData(true, 500, true, ContextualReadingDisposition.IgnoredTooBrief)]
    [InlineData(true, 3000, false, ContextualReadingDisposition.IgnoredNotCompleted)]
    public void Reading_IgnoredGates_ChangeNothing(bool visible, int millis, bool completed, ContextualReadingDisposition expected)
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var word = Guid.NewGuid();

        var result = engine.RecordContextualReading(new ContextualReadingObservation(
            Guid.NewGuid(), user, word, At, visible, TimeSpan.FromMilliseconds(millis), completed));

        Assert.Equal(expected, result.Disposition);
        Assert.Null(result.AppliedReview);
        Assert.Null(engine.GetProgress(user, word));
    }

    [Fact]
    public void NewWordAssessment_SecondInit_RejectedButRetryAllowed()
    {
        var engine = new InMemoryLearningEngine();
        var user = Guid.NewGuid();
        var word = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var first = engine.RecordNewWordAssessment(eventId, user, word, At, FsrsRating.Easy);
        var retry = engine.RecordNewWordAssessment(eventId, user, word, At, FsrsRating.Easy);

        Assert.Equal(first, retry);
        Assert.Throws<InvalidOperationException>(() =>
            engine.RecordNewWordAssessment(Guid.NewGuid(), user, word, At.AddMinutes(1), FsrsRating.Good));
    }
}
