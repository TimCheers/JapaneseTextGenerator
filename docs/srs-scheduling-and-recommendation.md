# SRS scheduling and recommendation cases

Canonical specification for the FSRS-7 scheduling and recommendation tests:

- `backend/KotobaApi.Tests/Fsrs7ConformanceTests.cs` — numerical conformance vectors
- `backend/KotobaApi.Tests/SrsSchedulingTests.cs` — cases S1–S21
- `backend/KotobaApi.Tests/SrsRecommendationTests.cs` — cases R1–R22
- `backend/KotobaApi.Tests/SrsIdempotencyTests.cs` — cases I1–I3

Below, assume:

```text
now = 2026-09-23 00:00 UTC
desired retention = 0.90
```

The exact interval produced by FSRS depends on the current memory state, so where exact FSRS numbers are unnecessary, the cases test the **required relationship** rather than inventing a specific number.

# A. Scheduling cases

Scheduling answers:

> After this word is reviewed, what is its new memory state and next `DueAt`?

---

## S1. Completely new word + Again

Word:

```text
食べる（たべる）
```

Before:

```text
Progress = null
```

User's first assessment:

```text
Again
```

Expected:

```text
A new FSRS state is created.
ReviewCount = 1
LapseCount += 0
LastReviewedAt = now
DueAt > now
```

A first-ever `Again` is **not** a lapse: a lapse means a previously learned
memory was forgotten, and a brand-new word simply hasn't been learned yet.
`Again` on an already learned word counts `LapseCount += 1` (see S8).

And compared with first-review Hard/Good/Easy:

```text
Again produces the earliest next review.
```

---

## S2. Completely new word + Hard

```text
飲む（のむ）
```

Before:

```text
Progress = null
```

First rating:

```text
Hard
```

Expected:

```text
new state created
ReviewCount = 1
LastReviewedAt = now
```

Relative interval:

```text
Again < Hard
```

---

## S3. Completely new word + Good

```text
行く（いく）
```

First rating:

```text
Good
```

Expected ordering:

```text
Again < Hard < Good
```

The word now has:

```text
Stability
FastStability
Difficulty
DueAt
```

---

## S4. Completely new word + Easy

```text
見る（みる）
```

First rating:

```text
Easy
```

Expected:

```text
Easy gives the longest initial interval.
```

Ordering:

```text
Again < Hard < Good < Easy
```

---

# Existing learned words

## S5. Successful Good review

```text
勉強する（べんきょうする）
```

Before:

```text
DueAt <= now
```

Rating:

```text
Good
```

Expected:

```text
Stability normally increases.
LastReviewedAt = now.
DueAt moves into the future.
ReviewCount += 1.
LapseCount unchanged.
```

---

## S6. Easy produces a later review than Good

Use identical starting states for two copies of:

```text
覚える（おぼえる）
```

Review A:

```text
Good
```

Review B:

```text
Easy
```

Required invariant:

```text
DueAt(Easy) > DueAt(Good)
```

This is better as a test than asserting some arbitrary number of days.

---

## S7. Hard produces an earlier review than Good

Same starting memory state:

```text
忘れる（わすれる）
```

Compare:

```text
Hard
Good
```

Expected:

```text
DueAt(Hard) < DueAt(Good)
```

---

## S8. Again after previously knowing a word

```text
続ける（つづける）
```

Suppose it has substantial existing stability.

Rating:

```text
Again
```

Expected:

```text
memory is weakened
LapseCount += 1
ReviewCount += 1
DueAt becomes substantially earlier
```

Important:

```text
It does NOT become a completely new word again.
```

Previous learning is still represented in the state.

---

## S9. A lapse can cause a same-day review

```text
決める（きめる）
```

At:

```text
2026-09-22 23:45 UTC
```

user gives:

```text
Again
```

FSRS-7 may produce something like:

```text
DueAt = later the same day / shortly afterward
```

For example:

```text
ReviewedAt = 23:45
DueAt      = 23:58
```

That is valid.

Do **not** round it to:

```text
tomorrow
```

FSRS-7 intervals are fractional.

---

## S10. Same-day second successful review

```text
調べる（しらべる）
```

Reviews:

```text
09:00 Good
15:00 Good
```

Expected:

```text
second review uses elapsed time = 6 hours
```

Not:

```text
0 days
```

This is precisely why timestamps matter.

---

# Retrievability effects

## S11. Good after a short interval

```text
作る（つくる）
```

Suppose the word was reviewed recently and:

```text
Retrievability ≈ very high
```

User says:

```text
Good
```

Expected:

```text
stability increases
```

but generally less dramatically than a successful recall after a much longer gap.

---

## S12. Good after a difficult long interval

Same starting word/state model:

```text
作る（つくる）
```

but much more time has elapsed.

Before review:

```text
Retrievability is substantially lower
```

User still answers:

```text
Good
```

Expected:

```text
successful difficult retrieval can produce stronger stability growth.
```

This is a core FSRS behavior.

---

# Difficulty behavior

## S13. Repeated Again makes a word harder

```text
間違える（まちがえる）
```

Sequence over time:

```text
Again
Again
Again
```

Expected:

```text
Difficulty trends upward.
```

Invariant:

```text
1 <= Difficulty <= 10
```

---

## S14. Repeated Easy can reduce difficulty

Start from a hard state (for example after an `Again`), then review:

```text
歩く（あるく）
```

with several successful Easy reviews.

Expected:

```text
Difficulty trends downward from the hard state.
```

Still:

```text
Difficulty >= 1
```

(Note: a brand-new word assessed Easy starts at the difficulty floor, so the
downward trend is only observable from a harder starting state.)

---

# Desired retention

## S15. Higher desired retention means earlier review

Use identical memory state for:

```text
考える（かんがえる）
```

Schedule once with:

```text
desired retention = 0.90
```

and once with:

```text
desired retention = 0.95
```

Required:

```text
DueAt(0.95) < DueAt(0.90)
```

Because maintaining 95% recall requires reviewing sooner.

---

## S16. Lower desired retention means later review

Same state:

```text
話す（はなす）
```

Compare:

```text
0.85
0.90
```

Expected:

```text
DueAt(0.85) > DueAt(0.90)
```

---

# Time correctness

## S17. Equivalent timestamps in different timezones

For:

```text
来る（くる）
```

These are the same instant:

```text
2026-09-23 09:00 +09:00
2026-09-23 00:00 UTC
```

Scheduling must produce identical results.

---

## S18. Review earlier than previous review is invalid

```text
帰る（かえる）
```

Stored:

```text
LastReviewedAt = 2026-09-23 10:00 UTC
```

Incoming review:

```text
ReviewedAt = 2026-09-23 09:00 UTC
```

Expected:

```text
Reject.
```

Do not silently clamp elapsed time to zero.

---

## S19. Exact same-time review

```text
読む（よむ）
```

If your application permits two legitimate events at the exact same timestamp:

```text
elapsed = 0
```

FSRS-7 can mathematically handle same-day intervals.

But at the application layer, I'd normally require distinct events and allow this only when it is genuinely intentional.

---

# Numerical safety

## S20. Very overdue word

```text
説明する（せつめいする）
```

Suppose:

```text
DueAt = 2026-06-01
now   = 2026-09-23
```

It is more than three months overdue.

Expected:

```text
Retrievability may be very low.
The scheduler still returns a valid result.
No NaN.
No overflow.
No crash.
```

---

## S21. Stability bounds

For:

```text
理解する（りかいする）
```

after an extreme review sequence:

```text
Again, Again, Easy, Again, Good, Easy, ...
```

always maintain:

```text
0.0001 <= Stability <= 36500
0.0001 <= FastStability <= 36500
```

---

# B. Recommendation cases

Recommendation answers:

> Given all of a user's words right now, which words should be selected for practice, and in what order?

For MVP:

```text
NEW:
    Progress == null

DUE:
    Progress != null
    AND DueAt <= now

NOT_DUE:
    Progress != null
    AND DueAt > now
```

A recommendation call is for a single user. Every candidate carries its
`UserId`/`WordId`, and attached progress must belong to the same user and
word with a complete scheduling state (`MemoryState`, `LastReviewedAt`,
`DueAt` present, `ReviewCount > 0`). Anything else is `Invalid` and fails
fast instead of being silently recommended. Duplicate `(user_id, word_id)`
rows are likewise rejected; that uniqueness is structural.

Default priority:

```text
1. DUE
2. NEW
3. NOT_DUE excluded
```

Among DUE:

```text
DueAt ASC
WordId ASC
```

Meaning:

> oldest due date = most overdue = first.

Exact timestamp ties are broken deterministically by `WordId`, which keeps
pagination and tests stable. Most-overdue is authoritative; no other signal
displaces it in v1.

---

# R1. New word

```text
泳ぐ（およぐ）
```

State:

```text
Progress = null
```

Classification:

```text
NEW
```

Eligible:

```text
Yes
```

But behind existing DUE words.

---

# R2. Learned but not due

```text
走る（はしる）
```

```text
DueAt = 2026-09-25 12:00
now   = 2026-09-23 00:00
```

Classification:

```text
NOT_DUE
```

Recommendation:

```text
Exclude.
```

---

# R3. Exactly due now

```text
待つ（まつ）
```

```text
DueAt = 2026-09-23 00:00
now   = 2026-09-23 00:00
```

Because:

```text
DueAt <= now
```

classification is:

```text
DUE
```

Not NOT_DUE.

---

# R4. One minute overdue

```text
持つ（もつ）
```

```text
DueAt = 2026-09-22 23:59
now   = 2026-09-23 00:00
```

Classification:

```text
DUE
```

---

# R5. Three days overdue

```text
買う（かう）
```

```text
DueAt = 2026-09-20 00:00
```

Classification:

```text
DUE
```

And it ranks ahead of `持つ`, because:

```text
Sep 20 < Sep 22 23:59
```

---

# R6. Most overdue wins, not the most recently lapsed

Suppose:

```text
買う
DueAt = Sep 20

見る
DueAt = Sep 22 23:50
```

Even if `見る` got there because of an `Again` lapse:

```text
1. 買う
2. 見る
```

under the simple MVP policy.

There is **no special lapse boost yet**.

---

# R7. Two words with identical DueAt

```text
書く（かく）
聞く（きく）
```

Both:

```text
DueAt = Sep 21 12:00
```

Tie-break is deterministic:

```text
lower WordId first
```

so repeated calls and pagination stay stable.

---

# R8. Completely identical scheduling priority

```text
開ける（あける）
閉める（しめる）
```

Suppose both have:

```text
same DueAt
```

Then the business ordering doesn't matter.

The deterministic final tie-breaker is:

```text
word_id ASC
```

That makes pagination/tests stable.

So actual sort is:

```text
DueAt ASC
WordId ASC
```

---

# R9. Due + new + future

Suppose Aiko has:

| Word | State            |
| ---- | ---------------- |
| 買う   | Due Sep 20       |
| 見る   | Due Sep 22 23:50 |
| 食べる  | NEW              |
| 飲む   | Due Sep 26       |
| 行く   | Due Sep 29       |

At Sep 23:

Recommended:

```text
1. 買う
2. 見る
3. 食べる
```

Excluded:

```text
飲む
行く
```

---

# R10. Only NEW words

Vocabulary:

```text
起きる（おきる）
寝る（ねる）
働く（はたらく）
```

All:

```text
Progress = null
```

Result:

```text
all are eligible as NEW
```

Their internal NEW order can simply be:

```text
word.created_at ASC
```

so the user's older pending words are introduced first.

---

# R11. Only NOT_DUE words

```text
教える（おしえる）
習う（ならう）
使う（つかう）
```

All have:

```text
DueAt > now
```

No NEW words.

Recommendation:

```text
[]
```

This is valid.

Do **not** pull future reviews forward simply to fill a batch.

---

# R12. Only DUE words

```text
会う（あう）       Due Sep 18
立つ（たつ）       Due Sep 19
座る（すわる）     Due Sep 21
休む（やすむ）     Due Sep 22
```

Recommendation:

```text
1. 会う
2. 立つ
3. 座る
4. 休む
```

---

# R13. Empty vocabulary

User has no words.

Result:

```text
[]
```

No exception.

---

# R14. Large backlog

Suppose Aiko has:

```text
80 DUE
20 NEW
300 NOT_DUE
```

and requested recommendation limit is:

```text
20
```

Result:

```text
top 20 DUE words
```

No NEW word should displace an overdue word under our default policy.

---

# R15. Fewer due words than batch size

Suppose:

```text
5 DUE
30 NEW
limit = 10
```

Result:

```text
5 DUE
+
5 NEW
```

---

# R16. Exact batch capacity

```text
10 DUE
50 NEW
limit = 10
```

Result:

```text
10 DUE
0 NEW
```

---

# R17. One due word + many new words

```text
忘れる   DUE
始める   NEW
終わる   NEW
入る     NEW
出る     NEW
```

With:

```text
limit = 3
```

Result:

```text
1. 忘れる
2. first NEW
3. second NEW
```

---

# R18. Future word remains excluded even if fragile

```text
選ぶ（えらぶ）
```

Suppose its current predicted recall is unusually low, but:

```text
DueAt = tomorrow
```

Under the basic recommender:

```text
NOT_DUE
```

Still excluded.

`DueAt` remains the scheduler's authoritative threshold.

---

# R19. Very overdue remains valid

```text
助ける（たすける）
```

```text
DueAt = three months ago
```

Result:

```text
DUE
```

Likely near the top of the queue.

No special "expired" state exists.

---

# R20. Same instant, different timezone

```text
話す
DueAt = 2026-09-23 09:00 +09:00
```

and:

```text
now = 2026-09-23 00:00 UTC
```

These are equal instants.

Therefore:

```text
DUE
```

---

# R21. Word with invalid/incomplete progress

Example:

```text
覚える
Progress exists
DueAt = null
```

but the word is supposedly already reviewed.

Don't classify this as NEW.

It's an invalid state.

Expected:

```text
fail validation / surface data integrity error
```

rather than silently recommending it.

The same applies to other incoherent shapes the engine itself never
produces but a future database loader must reject: missing `MemoryState`,
missing `LastReviewedAt`, `ReviewCount == 0` while a due date is set, or
progress belonging to a different user or word.

---

# R22. Duplicate word-progress rows

If somehow there are:

```text
(user=Aiko, word=食べる)
(user=Aiko, word=食べる)
```

twice in `word_progress`, recommendation behavior becomes ambiguous.

Prevent this structurally:

```sql
UNIQUE (user_id, word_id)
```

The recommender should never need to resolve duplicates.

---

# C. Idempotency cases that affect scheduling

These aren't recommendation rules, but they're essential because otherwise retries can move `DueAt` twice.

## I1. Identical retry

```text
Word: 読む
EventId: ABC
Rating: Good
ReviewedAt: Sep 23 10:00
```

Request arrives twice identically.

Expected:

```text
first → review applied
second → same result returned

ReviewCount only increases once.
DueAt unchanged by retry.
```

---

## I2. Same EventId, different rating

First:

```text
読む
ABC
Good
```

Retry:

```text
読む
ABC
Again
```

Expected:

```text
Reject.
```

---

## I3. Same EventId, different word

First:

```text
読む
ABC
Good
```

Second:

```text
書く
ABC
Good
```

Expected:

```text
Reject.
```

---

# D. Compact reference algorithm

## Scheduling

```csharp
ScheduleReview(word, rating, reviewedAt)
{
    if (word.Progress is null)
    {
        state = Fsrs.Initialize(rating);
    }
    else
    {
        if (reviewedAt < word.Progress.LastReviewedAt)
            throw InvalidReviewTime;

        elapsed = reviewedAt - word.Progress.LastReviewedAt;

        state = Fsrs.Review(
            word.Progress.MemoryState,
            elapsed,
            rating);
    }

    interval = Fsrs.NextInterval(
        state,
        desiredRetention: 0.90);

    return new Progress(
        state,
        LastReviewedAt: reviewedAt,
        DueAt: reviewedAt + interval);
}
```

## Recommendation

All candidates must belong to one user, and attached progress must belong
to the same user and word with a complete state; otherwise fail fast.

Conceptually:

```csharp
var due = words
    .Where(w => w.Progress != null)
    .Where(w => w.Progress!.DueAt <= now)
    .OrderBy(w => w.Progress!.DueAt)
    .ThenBy(w => w.Id);

var @new = words
    .Where(w => w.Progress == null)
    .OrderBy(w => w.CreatedAt)
    .ThenBy(w => w.Id);

return due
    .Concat(@new)
    .Take(limit);
```
