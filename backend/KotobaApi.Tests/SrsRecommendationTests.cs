using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Learning;
using Xunit;

namespace KotobaApi.Tests;

/// <summary>
/// Recommendation cases R1-R22 from docs/tests.md, against <see cref="SrsRecommendation"/>.
/// Fixed clock: now = 2026-09-23 00:00 UTC.
/// </summary>
public sealed class SrsRecommendationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Fsrs7MemoryState AnyState = new(5.0, 4.0, 5.0);

    private static SrsCandidate Candidate(Guid word, DateTimeOffset createdAt, WordLearningProgress? progress) =>
        new(word, createdAt, progress);

    private static WordLearningProgress Reviewed(Guid word, DateTimeOffset dueAt, DateTimeOffset? lastReviewedAt = null) =>
        new(User, word, AnyState, lastReviewedAt ?? dueAt.AddDays(-5), dueAt, 2, 0);

    // R1. New word (no progress) is eligible but behind DUE words (covered with R9).
    [Fact]
    public void R1_NullProgress_ClassifiedNew()
    {
        var candidate = Candidate(Guid.NewGuid(), Now, null);

        Assert.Equal(SrsRecommendationKind.New, SrsRecommendation.Classify(candidate, Now));
    }

    // R2. Learned but not due is excluded.
    [Fact]
    public void R2_FutureDue_Excluded()
    {
        var word = Guid.NewGuid();
        var candidates = new[] { Candidate(word, Now.AddDays(-10), Reviewed(word, new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero))) };

        Assert.Equal(SrsRecommendationKind.NotDue, SrsRecommendation.Classify(candidates[0], Now));
        Assert.Empty(SrsRecommendation.OrderForReview(candidates, Now, 10));
    }

    // R3. Exactly due now counts as DUE (DueAt <= now).
    [Fact]
    public void R3_ExactlyDueNow_IsDue()
    {
        var candidate = Candidate(Guid.NewGuid(), Now.AddDays(-10), Reviewed(Guid.NewGuid(), Now));

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
        Assert.Single(SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    // R4. One minute overdue is DUE.
    [Fact]
    public void R4_OneMinuteOverdue_IsDue()
    {
        var candidate = Candidate(Guid.NewGuid(), Now.AddDays(-10), Reviewed(Guid.NewGuid(), Now.AddMinutes(-1)));

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
    }

    // R5/R6. Oldest DueAt first; no special lapse boost in MVP.
    [Fact]
    public void R5_R6_MostOverdueFirst()
    {
        var threeDaysOverdue = Candidate(Guid.NewGuid(), Now.AddDays(-30), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero)));
        var minutesOverdue = Candidate(Guid.NewGuid(), Now.AddDays(-30), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 22, 23, 50, 0, TimeSpan.Zero)));

        var ordered = SrsRecommendation.OrderForReview(new[] { minutesOverdue, threeDaysOverdue }, Now, 10);

        Assert.Equal(new[] { threeDaysOverdue.WordId, minutesOverdue.WordId }, ordered.Select(c => c.WordId));
    }

    // R7. Identical DueAt breaks tie by lower retrievability.
    [Fact]
    public void R7_SameDueAt_LowerRetrievabilityFirst()
    {
        var dueAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var kaku = Guid.NewGuid();
        var kiku = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(kaku, Now.AddDays(-30), Reviewed(kaku, dueAt)),
            Candidate(kiku, Now.AddDays(-30), Reviewed(kiku, dueAt)),
        };
        var retrievability = new Dictionary<Guid, double> { [kaku] = 0.68, [kiku] = 0.54 };

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10, retrievability);

        Assert.Equal(new[] { kiku, kaku }, ordered.Select(c => c.WordId));
    }

    // R8. Fully identical priority falls back to WordId for determinism.
    [Fact]
    public void R8_IdenticalPriority_WordIdOrder()
    {
        var dueAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var lower = first.CompareTo(second) < 0 ? first : second;
        var higher = lower == first ? second : first;

        var candidates = new[]
        {
            Candidate(higher, Now.AddDays(-30), Reviewed(higher, dueAt)),
            Candidate(lower, Now.AddDays(-30), Reviewed(lower, dueAt)),
        };

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10);

        Assert.Equal(new[] { lower, higher }, ordered.Select(c => c.WordId));
    }

    // R9. Due + new + future: DUE by age, then NEW, future excluded.
    [Fact]
    public void R9_MixedDeck_DueThenNewFutureExcluded()
    {
        var kau = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero)));
        var miru = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 22, 23, 50, 0, TimeSpan.Zero)));
        var taberu = Candidate(Guid.NewGuid(), Now.AddDays(-5), null);
        var nomu = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero)));
        var iku = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero)));

        var ordered = SrsRecommendation.OrderForReview(new[] { iku, nomu, taberu, miru, kau }, Now, 10);

        Assert.Equal(new[] { kau.WordId, miru.WordId, taberu.WordId }, ordered.Select(c => c.WordId));
    }

    // R10. Only NEW words: eligible, ordered by CreatedAt.
    [Fact]
    public void R10_OnlyNew_AllEligibleByCreatedAt()
    {
        var oldest = Candidate(Guid.NewGuid(), Now.AddDays(-30), null);
        var middle = Candidate(Guid.NewGuid(), Now.AddDays(-20), null);
        var newest = Candidate(Guid.NewGuid(), Now.AddDays(-10), null);

        var ordered = SrsRecommendation.OrderForReview(new[] { newest, oldest, middle }, Now, 10);

        Assert.Equal(new[] { oldest.WordId, middle.WordId, newest.WordId }, ordered.Select(c => c.WordId));
    }

    // R11. Only NOT_DUE words: empty, never pulled forward.
    [Fact]
    public void R11_OnlyNotDue_Empty()
    {
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), Now.AddDays(-30), Reviewed(Guid.NewGuid(), Now.AddDays(1))),
            Candidate(Guid.NewGuid(), Now.AddDays(-30), Reviewed(Guid.NewGuid(), Now.AddDays(2))),
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
            .Select(d => { var w = Guid.NewGuid(); return Candidate(w, Now.AddDays(-40), Reviewed(w, d)); })
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
            var w = Guid.NewGuid();
            candidates.Add(Candidate(w, Now.AddDays(-100), Reviewed(w, Now.AddDays(-80 + i))));
        }
        for (var i = 0; i < 20; i++)
        {
            candidates.Add(Candidate(Guid.NewGuid(), Now.AddDays(-20 + i), null));
        }
        for (var i = 0; i < 300; i++)
        {
            var w = Guid.NewGuid();
            candidates.Add(Candidate(w, Now.AddDays(-100), Reviewed(w, Now.AddDays(1 + i))));
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
            var w = Guid.NewGuid();
            candidates.Add(Candidate(w, Now.AddDays(-40), Reviewed(w, Now.AddDays(-5 + i))));
        }
        for (var i = 0; i < 30; i++)
        {
            candidates.Add(Candidate(Guid.NewGuid(), Now.AddDays(-30 + i), null));
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
            var w = Guid.NewGuid();
            candidates.Add(Candidate(w, Now.AddDays(-40), Reviewed(w, Now.AddDays(-10 + i))));
        }
        for (var i = 0; i < 50; i++)
        {
            candidates.Add(Candidate(Guid.NewGuid(), Now.AddDays(-50 + i), null));
        }

        var ordered = SrsRecommendation.OrderForReview(candidates, Now, 10);

        Assert.Equal(10, ordered.Count);
        Assert.All(ordered, c => Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(c, Now)));
    }

    // R17. One due + many new with small limit: due first, then oldest new.
    [Fact]
    public void R17_OneDueManyNew_DueThenOldestNew()
    {
        var due = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), Now.AddDays(-2)));
        var newOldest = Candidate(Guid.NewGuid(), Now.AddDays(-30), null);
        var newMiddle = Candidate(Guid.NewGuid(), Now.AddDays(-20), null);
        var newNewest1 = Candidate(Guid.NewGuid(), Now.AddDays(-10), null);
        var newNewest2 = Candidate(Guid.NewGuid(), Now.AddDays(-5), null);

        var ordered = SrsRecommendation.OrderForReview(
            new[] { newNewest2, newNewest1, newMiddle, newOldest, due }, Now, 3);

        Assert.Equal(new[] { due.WordId, newOldest.WordId, newMiddle.WordId }, ordered.Select(c => c.WordId));
    }

    // R18. Fragile but future word stays excluded; DueAt is authoritative.
    [Fact]
    public void R18_FutureWordWithLowRetrievability_StillExcluded()
    {
        var futureWord = Guid.NewGuid();
        var future = Candidate(futureWord, Now.AddDays(-40), Reviewed(futureWord, Now.AddDays(1)));
        var due = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), Now.AddDays(-1)));
        var retrievability = new Dictionary<Guid, double> { [futureWord] = 0.01 };

        var ordered = SrsRecommendation.OrderForReview(new[] { future, due }, Now, 10, retrievability);

        Assert.Equal(new[] { due.WordId }, ordered.Select(c => c.WordId));
    }

    // R19. Very overdue word is DUE near the top; no expiry state.
    [Fact]
    public void R19_ThreeMonthsOverdue_DueFirst()
    {
        var ancient = Candidate(Guid.NewGuid(), Now.AddDays(-200), Reviewed(Guid.NewGuid(), Now.AddDays(-90)));
        var recent = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), Now.AddDays(-1)));

        var ordered = SrsRecommendation.OrderForReview(new[] { recent, ancient }, Now, 10);

        Assert.Equal(new[] { ancient.WordId, recent.WordId }, ordered.Select(c => c.WordId));
    }

    // R20. Same instant in another timezone counts as DUE.
    [Fact]
    public void R20_DueAtInJstEqualToNowUtc_IsDue()
    {
        var dueJst = new DateTimeOffset(2026, 9, 23, 9, 0, 0, TimeSpan.FromHours(9));
        var candidate = Candidate(Guid.NewGuid(), Now.AddDays(-40), Reviewed(Guid.NewGuid(), dueJst));

        Assert.Equal(SrsRecommendationKind.Due, SrsRecommendation.Classify(candidate, Now));
    }

    // R21. Progress with null DueAt is invalid and fails fast.
    [Fact]
    public void R21_NullDueAtWithProgress_InvalidAndThrows()
    {
        var word = Guid.NewGuid();
        var candidate = Candidate(word, Now.AddDays(-40), new WordLearningProgress(User, word, AnyState, Now.AddDays(-5), null, 2, 0));

        Assert.Equal(SrsRecommendationKind.Invalid, SrsRecommendation.Classify(candidate, Now));
        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(new[] { candidate }, Now, 10));
    }

    // R22. Duplicate word rows are rejected; UNIQUE (user_id, word_id) is structural.
    [Fact]
    public void R22_DuplicateWordId_Throws()
    {
        var word = Guid.NewGuid();
        var candidates = new[]
        {
            Candidate(word, Now.AddDays(-40), Reviewed(word, Now.AddDays(-2))),
            Candidate(word, Now.AddDays(-40), Reviewed(word, Now.AddDays(-1))),
        };

        Assert.Throws<InvalidOperationException>(() =>
            SrsRecommendation.OrderForReview(candidates, Now, 10));
    }
}
