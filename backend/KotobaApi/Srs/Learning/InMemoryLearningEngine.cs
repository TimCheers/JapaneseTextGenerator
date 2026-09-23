using KotobaApi.Srs.Domain;
using KotobaApi.Srs.Reading;
using KotobaApi.Srs.Scheduling;

namespace KotobaApi.Srs.Learning;

public sealed record ReviewCommand(
    Guid EventId,
    Guid UserId,
    Guid WordId,
    DateTimeOffset ReviewedAt,
    FsrsRating Rating,
    ReviewSource Source);

public sealed record AppliedReview(
    ReviewEvent Event,
    WordLearningProgress ProgressAfter);

public sealed record ContextualReadingResult(
    ContextualReadingDisposition Disposition,
    AppliedReview? AppliedReview);

/// <summary>
/// Thread-safe in-memory orchestration layer for early product testing.
/// Replace this class with a persistence-backed transaction boundary later; keep the scheduler unchanged.
/// Registered as a singleton while the store is in-memory so progress survives across requests.
/// Reset on restart by design.
/// </summary>
public sealed class InMemoryLearningEngine
{
    private readonly object _gate = new();
    private readonly IFsrsScheduler _scheduler;
    private readonly ReadingEngagementPolicy _readingPolicy;
    private readonly Dictionary<WordKey, WordLearningProgress> _progress = [];
    private readonly Dictionary<WordKey, List<ReviewEvent>> _history = [];
    private readonly Dictionary<Guid, AppliedReview> _resultsByEventId = [];

    public InMemoryLearningEngine(
        IFsrsScheduler? scheduler = null,
        ReadingEngagementPolicy? readingPolicy = null)
    {
        _scheduler = scheduler ?? new Fsrs7Scheduler();
        _readingPolicy = readingPolicy ?? new ReadingEngagementPolicy();
    }

    public AppliedReview ApplyReview(ReviewCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdentifiers(command.EventId, command.UserId, command.WordId);

        lock (_gate)
        {
            if (_resultsByEventId.TryGetValue(command.EventId, out var existingResult))
            {
                EnsureIdempotentReplayMatches(command, existingResult.Event);
                return existingResult;
            }

            var key = new WordKey(command.UserId, command.WordId);
            var previous = _progress.GetValueOrDefault(key) ?? WordLearningProgress.New(command.UserId, command.WordId);

            var schedule = _scheduler.Schedule(new FsrsScheduleRequest(
                previous.MemoryState,
                previous.LastReviewedAt,
                command.ReviewedAt,
                command.Rating));

            var normalizedReviewedAt = command.ReviewedAt.ToUniversalTime();
            var reviewEvent = new ReviewEvent(
                command.EventId,
                command.UserId,
                command.WordId,
                normalizedReviewedAt,
                command.Rating,
                command.Source,
                schedule.ElapsedDays,
                schedule.RetrievabilityBeforeReview,
                schedule.State,
                schedule.DesiredRetention,
                schedule.IntervalDays,
                schedule.DueAt,
                _scheduler.AlgorithmId,
                schedule.WasFirstReview);

            var nextProgress = previous with
            {
                MemoryState = schedule.State,
                LastReviewedAt = normalizedReviewedAt,
                DueAt = schedule.DueAt,
                ReviewCount = previous.ReviewCount + 1,
                LapseCount = previous.LapseCount + (command.Rating == FsrsRating.Again && !schedule.WasFirstReview ? 1 : 0),
            };

            _progress[key] = nextProgress;
            if (!_history.TryGetValue(key, out var events))
            {
                events = [];
                _history.Add(key, events);
            }

            events.Add(reviewEvent);
            var result = new AppliedReview(reviewEvent, nextProgress);
            _resultsByEventId.Add(command.EventId, result);
            return result;
        }
    }

    /// <summary>
    /// Normal engaged reading maps to Good, as agreed for the product experiment.
    /// The event remains distinguishable through ReviewSource.ContextualReading.
    /// </summary>
    public ContextualReadingResult RecordContextualReading(ContextualReadingObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var disposition = _readingPolicy.Evaluate(observation);
        if (disposition != ContextualReadingDisposition.Applied)
        {
            return new ContextualReadingResult(disposition, null);
        }

        var applied = ApplyReview(new ReviewCommand(
            observation.EventId,
            observation.UserId,
            observation.WordId,
            observation.ObservedAt,
            FsrsRating.Good,
            ReviewSource.ContextualReading));

        return new ContextualReadingResult(disposition, applied);
    }

    /// <summary>A meaning reveal is automatically treated as Hard.</summary>
    public AppliedReview RecordMeaningReveal(
        Guid eventId,
        Guid userId,
        Guid wordId,
        DateTimeOffset revealedAt) =>
        ApplyReview(new ReviewCommand(
            eventId,
            userId,
            wordId,
            revealedAt,
            FsrsRating.Hard,
            ReviewSource.MeaningRevealed));


    /// <summary>
    /// Initializes a newly discovered word from the learner's explicit self-assessment.
    /// A word that already has review history cannot be initialized again through this method.
    /// </summary>
    public AppliedReview RecordNewWordAssessment(
        Guid eventId,
        Guid userId,
        Guid wordId,
        DateTimeOffset assessedAt,
        FsrsRating rating)
    {
        var command = new ReviewCommand(
            eventId,
            userId,
            wordId,
            assessedAt,
            rating,
            ReviewSource.NewWordAssessment);

        lock (_gate)
        {
            // Preserve idempotency for network retries of the original initialization event.
            if (_resultsByEventId.ContainsKey(eventId))
            {
                return ApplyReview(command);
            }

            var key = new WordKey(userId, wordId);
            if (_progress.TryGetValue(key, out var progress) && progress.ReviewCount > 0)
            {
                throw new InvalidOperationException("The word already has review history and is no longer new.");
            }

            // Monitor locks are re-entrant; ApplyReview keeps the same atomic in-memory boundary.
            return ApplyReview(command);
        }
    }

    public WordLearningProgress? GetProgress(Guid userId, Guid wordId)
    {
        lock (_gate)
        {
            return _progress.GetValueOrDefault(new WordKey(userId, wordId));
        }
    }

    public IReadOnlyList<ReviewEvent> GetHistory(Guid userId, Guid wordId)
    {
        lock (_gate)
        {
            return _history.TryGetValue(new WordKey(userId, wordId), out var events)
                ? events.ToArray()
                : Array.Empty<ReviewEvent>();
        }
    }

    private static void EnsureIdempotentReplayMatches(ReviewCommand command, ReviewEvent existing)
    {
        if (command.UserId != existing.UserId
            || command.WordId != existing.WordId
            || command.ReviewedAt.ToUniversalTime() != existing.ReviewedAt
            || command.Rating != existing.Rating
            || command.Source != existing.Source)
        {
            throw new InvalidOperationException(
                $"EventId {command.EventId} was already used for a different review payload.");
        }
    }

    private static void ValidateIdentifiers(Guid eventId, Guid userId, Guid wordId)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (wordId == Guid.Empty) throw new ArgumentException("WordId cannot be empty.", nameof(wordId));
    }

    private readonly record struct WordKey(Guid UserId, Guid WordId);
}
