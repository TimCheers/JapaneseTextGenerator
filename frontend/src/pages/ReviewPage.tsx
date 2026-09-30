import { useContext, useEffect, useState } from "react";
import { AuthContext } from "../context/AuthContext";
import type { WordProgress, FsrsRating } from "../api/wordProgress";
import { getDueWords, submitReview, FSRS_RATING } from "../api/wordProgress";
import "../styles/ReviewPage.css";

export function ReviewPage() {
    const { user } = useContext(AuthContext);
    const [dueWords, setDueWords] = useState<WordProgress[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState("");
    const [isRevealed, setIsRevealed] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        if (!user) return;
        getDueWords(user.id)
            .then((result) => setDueWords(result))
            .catch((err) => setError(err instanceof Error ? err.message : "Something went wrong"))
            .finally(() => setIsLoading(false));
    }, [user]);

    async function handleRate(rating: FsrsRating) {
        if (!user || isSubmitting) return;
        const current = dueWords[0];
        if (!current) return;

        setIsSubmitting(true);
        try {
            await submitReview(user.id, current.wordId, rating);
            setDueWords((prev) => prev.slice(1));
            setIsRevealed(false);
        } catch (err) {
            setError(err instanceof Error ? err.message : "Something went wrong");
        } finally {
            setIsSubmitting(false);
        }
    }

    if (isLoading) {
        return <p>Загрузка...</p>;
    }

    const current = dueWords[0];

    return (
        <div className="review-page">
            <h1>Review</h1>
            {error && <p className="review-error">{error}</p>}
            {!current ? (
                <p>There's nothing to review today.</p>
            ) : (
                <div className="review-card">
                    <p className="review-term">{current.term}</p>
                    {isRevealed ? (
                        <>
                            {current.reading && <p className="review-reading">{current.reading}</p>}
                            <p className="review-meaning">{current.meaning}</p>
                            <div className="review-buttons">
                                <button onClick={() => handleRate(FSRS_RATING.Again)} disabled={isSubmitting}>
                                    Again
                                </button>
                                <button onClick={() => handleRate(FSRS_RATING.Hard)} disabled={isSubmitting}>
                                    Hard
                                </button>
                                <button onClick={() => handleRate(FSRS_RATING.Good)} disabled={isSubmitting}>
                                    Good
                                </button>
                                <button onClick={() => handleRate(FSRS_RATING.Easy)} disabled={isSubmitting}>
                                    Easy
                                </button>
                            </div>
                        </>
                    ) : (
                        <button onClick={() => setIsRevealed(true)}>Show answer</button>
                    )}
                </div>
            )}
        </div>
    );
}