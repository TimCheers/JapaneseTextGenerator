using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Learning;
using Xunit;

namespace KotobaApi.Tests;

/// <summary>
/// Recommendation cases R1-R22 from docs/srs-scheduling-and-recommendation.md,
/// against <see cref="SrsRecommendation"/>.
/// Fixed clock: now = 2026-09-23 00:00 UTC.
/// </summary>
public sealed class SrsRecommendationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Fsrs7MemoryState AnyState = new(5.0, 4.0, 5.0);

    private static SrsCandidate NewCandidate(DateTimeOffset createdAt) =>
        new(User, Guid.NewGuid(), createdAt, null);

    private static SrsCandidate ReviewedCandidate(
        DateTimeOffset createdAt,
        DateTimeOffset dueAt,
        DateTimeOffset? lastReviewedAt = null)
    {
        var word = Guid.NewGuid();
        return new SrsCandidate(
            User,
            word,
            createdAt,
            new WordLearningProgress(User, word, AnyState, lastReviewedAt ?? dueAt.AddDays(-5), dueAt, 2, 0));
    }

    // R1. New word (no progress) is eligible but behind DUE words (covered with R9).
    [Fact]
    public void R1_NullProgress_ClassifiedNew()
    {
        var candidate = NewCandidate(Now);

        Assert.Equal(SrsRecommendationKind.New, SrsRecommendation.Classify(candidate, Now));
    }

    // R2. Learned but not due is excluded.
    [Fact]
    public void R2_FutureDue_Excluded()
    {
        var candidate = ReviewedCandidate(Now.AddDays(-10), new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(SrsRecommendationKind.NotDue, SrsRecommendation.Classify(candidate, Now));
        Assert.Empty(SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    // R3. Exactly due now counts as DUE (DueAt <= now).
    [Fact]
    public void R3_ExactlyDueNow_IsDue()
    {
        var candidate = ReviewedCandidate(Now.AddDays(-10), Now);

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
        Assert.Single(SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    // R4. One minute overdue is DUE.
    [Fact]
    public void R4_OneMinuteOverdue_IsDue()
    {
        var candidate = ReviewedCandidate(Now.AddDays(-10), Now.AddMinutes(-1));

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
    }

    // R5/R6. Oldest DueAt first; no special lapse boost in MVP.
    [Fact]
    public void R5_R6_MostOverdueFirst()
    {
        var threeDaysOverdue = ReviewedCandidate(Now.AddDays(-30), new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero));
        var minutesOverdue = ReviewedCandidate(Now.AddDays(-30), new DateTimeOffset(2026, 9, 22, 23, 50, 0, TimeSpan.Zero));

        var ordered = SrsRecommendation.OrderForReview(new[] { minutesOverdue, threeDaysOverdue }, Now, 10);

        Assert.Equal(new[] { threeDaysOverdue.WordId, minutesOverdue.WordId }, ordered.Select(c => c.WordId));
    }

    // R7. Identical DueAt falls back to WordId for a deterministic order.
    [Fact]
    public void R7_SameDueAt_WordIdOrder()
    {
        var dueAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var first = ReviewedCandidate(Now.AddDays(-30), dueAt);
        var second = ReviewedCandidate(Now.AddDays(-30), dueAt);
        var lower = first.WordId.CompareTo(second.WordId) < 0 ? first : second;
        var higher = lower == first ? second : first;

        var ordered = SrsRecommendation.OrderForReview(new[] { higher, lower }, Now, 10);

        Assert.Equal(new[] { lower.WordId, higher.WordId }, ordered.Select(c => c.WordId));
    }

    // R8. Fully identical priority is stable regardless of input order.
    [Fact]
    public void R8_IdenticalPriority_SameOutputEitherInputOrder()
    {
        var dueAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var first = ReviewedCandidate(Now.AddDays(-30), dueAt);
        var second = ReviewedCandidate(Now.AddDays(-30), dueAt);

        var forward = SrsRecommendation.OrderForReview(new[] { first, second }, Now, 10);
        var reversed = SrsRecommendation.OrderForReview(new[] { second, first }, Now, 10);

        Assert.Equal(forward.Select(c => c.WordId), reversed.Select(c => c.WordId));
    }

    // R9. Due + new + future: DUE by age, then NEW, future excluded.
    [Fact]
    public void R9_MixedDeck_DueThenNewFutureExcluded()
    {
        var kau = ReviewedCandidate(Now.AddDays(-40), new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero));
        var miru = ReviewedCandidate(Now.AddDays(-40), new DateTimeOffset(2026, 9, 22, 23, 50, 0, TimeSpan.Zero));
        var taberu = NewCandidate(Now.AddDays(-5));
        var nomu = ReviewedCandidate(Now.AddDays(-40), new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero));
        var iku = ReviewedCandidate(Now.AddDays(-40), new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero));

        var ordered = SrsRecommendation.OrderForReview(new[] { iku, nomu, taberu, miru, kau }, Now, 10);

        Assert.Equal(new[] { kau.WordId, miru.WordId, taberu.WordId }, ordered.Select(c => c.WordId));
    }

    // R10. Only NEW words: eligible, ordered by CreatedAt.
    [Fact]
    public void R10_OnlyNew_AllEligibleByCreatedAt()
    {
        var oldest = NewCandidate(Now.AddDays(-30));
        var middle = NewCandidate(Now.AddDays(-20));
        var newest = NewCandidate(Now.AddDays(-10));

        var ordered = SrsRecommendation.OrderForReview(new[] { newest, oldest, middle }, Now, 10);

        Assert.Equal(new[] { oldest.WordId, middle.WordId, newest.WordId }, ordered.Select(c => c.WordId));
    }

    // R11. Only NOT_DUE words: empty, never pulled forward.
    [Fact]
    public void R11_OnlyNotDue_Empty()
    {
        var candidates = new[]
        {
            ReviewedCandidate(Now.AddDays(-30), Now.AddDays(1)),
            ReviewedCandidate(Now.AddDays(-30), Now.AddDays(2)),
        };

        Assert.Empty(SrsRecommendation.OrderForReview(candidates, Now, 10));
    }

    // R12. Only DUE words: oldest first.
    [Fact]
    public void R12_OnlyDue_OldestFirst()
    {
        var dueDates = new[]
        {
            new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero),
        };
        var candidates = dueDates
            .Select(d => ReviewedCandidate(Now.AddDays(-40), d))
            .Reverse()
            .ToList();

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10);

        Assert.Equal(dueDates, ordered.Select(c => c.Progress!.DueAt!.Value));
    }

    // R13. Empty vocabulary: empty, no exception.
    [Fact]
    public void R13_EmptyVocabulary_Empty()
    {
        Assert.Empty(SrsRecommendation.OrderForReview(Array.Empty<SrsCandidate>(), Now, 10));
    }

    // R14. Large backlog: DUE fills the whole batch, NEW never displaces overdue.
    [Fact]
    public void R14_LargeBacklog_TopDueOnly()
    {
        var candidates = new List<SrsCandidate>();
        for (var i = 0; i < 80; i++)
        {
            candidates.Add(ReviewedCandidate(Now.AddDays(-100), Now.AddDays(-80 + i)));
        }
        for (var i = 0; i < 20; i++)
        {
            candidates.Add(NewCandidate(Now.AddDays(-20 + i)));
        }
        for (var i = 0; i < 300; i++)
        {
            candidates.Add(ReviewedCandidate(Now.AddDays(-100), Now.AddDays(1 + i)));
        }

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 20);

        Assert.Equal(20, ordered.Count);
        Assert.All(ordered, c => Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(c, Now)));
        Assert.Equal(ordered.Select(c => c.Progress!.DueAt!.Value), ordered.Select(c => c.Progress!.DueAt!.Value).OrderBy(d => d));
    }

    // R15. Fewer due than batch: 5 DUE + 5 NEW.
    [Fact]
    public void R15_FewerDueThanBatch_DuePlusNew()
    {
        var candidates = new List<SrsCandidate>();
        for (var i = 0; i < 5; i++)
        {
            candidates.Add(ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-5 + i)));
        }
        for (var i = 0; i < 30; i++)
        {
            candidates.Add(NewCandidate(Now.AddDays(-30 + i)));
        }

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10);

        Assert.Equal(10, ordered.Count);
        Assert.Equal(5, ordered.Take(5).Count(c => SrsRecommendation.Classify(c, Now) == SrsRecommendationKind.Due));
        Assert.Equal(5, ordered.Skip(5).Count(c => SrsRecommendation.Classify(c, Now) == SrsRecommendationKind.New));
    }

    // R16. Exact batch capacity: DUE fills it, no NEW.
    [Fact]
    public void R16_ExactCapacity_AllDue()
    {
        var candidates = new List<SrsCandidate>();
        for (var i = 0; i < 10; i++)
        {
            candidates.Add(ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-10 + i)));
        }
        for (var i = 0; i < 50; i++)
        {
            candidates.Add(NewCandidate(Now.AddDays(-50 + i)));
        }

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10);

        Assert.Equal(10, ordered.Count);
        Assert.All(ordered, c => Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(c, Now)));
    }

    // R17. One due + many new with small limit: due first, then oldest new.
    [Fact]
    public void R17_OneDueManyNew_DueThenOldestNew()
    {
        var due = ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-2));
        var newOldest = NewCandidate(Now.AddDays(-30));
        var newMiddle = NewCandidate(Now.AddDays(-20));
        var newNewest1 = NewCandidate(Now.AddDays(-10));
        var newNewest2 = NewCandidate(Now.AddDays(-5));

        var ordered = SrsRecommendation.OrderForReview(
            new[] { newNewest2, newNewest1, newMiddle, newOldest, due }, Now, 3);

        Assert.Equal(new[] { due.WordId, newOldest.WordId, newMiddle.WordId }, ordered.Select(c => c.WordId));
    }

    // R18. A word due tomorrow stays excluded even when barely in the future.
    [Fact]
    public void R18_FutureWord_StillExcluded()
    {
        var future = ReviewedCandidate(Now.AddDays(-40), Now.AddMinutes(1));
        var due = ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-1));

        var ordered = SrsRecommendation.OrderForReview(new[] { future, due }, Now, 10);

        Assert.Equal(new[] { due.WordId }, ordered.Select(c => c.WordId));
    }

    // R19. Very overdue word is DUE near the top; no expiry state.
    [Fact]
    public void R19_ThreeMonthsOverdue_DueFirst()
    {
        var ancient = ReviewedCandidate(Now.AddDays(-200), Now.AddDays(-90));
        var recent = ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-1));

        var ordered = SrsRecommendation.OrderForReview(new[] { recent, ancient }, Now, 10);

        Assert.Equal(new[] { ancient.WordId, recent.WordId }, ordered.Select(c => c.WordId));
    }

    // R20. Same instant in another timezone counts as DUE.
    [Fact]
    public void R20_DueAtInJstEqualToNowUtc_IsDue()
    {
        var dueJst = new DateTimeOffset(2026, 9, 23, 9, 0, 0, TimeSpan.FromHours(9));
        var candidate = ReviewedCandidate(Now.AddDays(-40), dueJst);

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
    }

    // R21. Progress with null DueAt is invalid and fails fast.
    [Fact]
    public void R21_NullDueAtWithProgress_InvalidAndThrows()
    {
        var word = Guid.NewGuid();
        var candidate = new SrsCandidate(
            User, word, Now.AddDays(-40),
            new WordLearningProgress(User, word, AnyState, Now.AddDays(-5), null, 2, 0));

        Assert.Equal(SrsRecommendationKind.Invalid, SrsRecommendation.Classify(candidate, Now));
        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    // R21b. Other incoherent progress shapes are invalid too.
    [Fact]
    public void R21b_IncoherentProgress_InvalidAndThrows()
    {
        var incoherent = new List<SrsCandidate>();
        foreach (var progress in IncoherentProgressShapes())
        {
            incoherent.Add(new SrsCandidate(progress.UserId, progress.WordId, Now.AddDays(-40), progress));
        }

        Assert.All(incoherent, c => Assert.Equal(SrsRecommendationKind.Invalid, SrsRecommendation.Classify(c, Now)));
        foreach (var candidate in incoherent)
        {
            Assert.Throws<InvalidOperationException>(() =>
                SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
        }
    }

    // R22. Duplicate word rows are rejected; UNIQUE (user_id, word_id) is structural.
    [Fact]
    public void R22_DuplicateWordId_Throws()
    {
        var word = Guid.NewGuid();
        var progress = new WordLearningProgress(User, word, AnyState, Now.AddDays(-5), Now.AddDays(-2), 2, 0);
        var updated = progress with { DueAt = Now.AddDays(-1) };
        var candidates = new[]
        {
            new SrsCandidate(User, word, Now.AddDays(-40), progress),
            new SrsCandidate(User, word, Now.AddDays(-40), updated),
        };

        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(candidates, Now, 10));
    }

    [Fact]
    public void ProgressForDifferentWord_IsRejected()
    {
        var candidate = new SrsCandidate(
            User, Guid.NewGuid(), Now.AddDays(-40),
            new WordLearningProgress(User, Guid.NewGuid(), AnyState, Now.AddDays(-5), Now.AddDays(-1), 2, 0));

        Assert.Equal(SrsRecommendationKind.Invalid, SrsRecommendation.Classify(candidate, Now));
        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    [Fact]
    public void ProgressForDifferentUser_IsRejected()
    {
        var word = Guid.NewGuid();
        var candidate = new SrsCandidate(
            User, word, Now.AddDays(-40),
            new WordLearningProgress(Guid.NewGuid(), word, AnyState, Now.AddDays(-5), Now.AddDays(-1), 2, 0));

        Assert.Equal(SrsRecommendationKind.Invalid, SrsRecommendation.Classify(candidate, Now));
        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    [Fact]
    public void CandidatesAcrossUsers_AreRejected()
    {
        var first = ReviewedCandidate(Now.AddDays(-40), Now.AddDays(-2));
        var otherUser = Guid.NewGuid();
        var otherWord = Guid.NewGuid();
        var second = new SrsCandidate(
            otherUser, otherWord, Now.AddDays(-40),
            new WordLearningProgress(otherUser, otherWord, AnyState, Now.AddDays(-5), Now.AddDays(-1), 2, 0));

        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(new[] { first, second }, Now, 10));
    }

    private static IEnumerable<WordLearningProgress> IncoherentProgressShapes()
    {
        var word = Guid.NewGuid();
        // Missing memory state.
        yield return new WordLearningProgress(User, word, null, Now.AddDays(-5), Now.AddDays(-1), 2, 0);
        // Missing last-reviewed timestamp.
        yield return new WordLearningProgress(User, word, AnyState, null, Now.AddDays(-1), 2, 0);
        // Zero reviews cannot own a due date.
        yield return new WordLearningProgress(User, word, AnyState, Now.AddDays(-5), Now.AddDays(-1), 0, 0);
    }
}
