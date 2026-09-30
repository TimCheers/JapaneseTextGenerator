import { getAuthHeaders } from "./authHeaders"

export type FsrsRating = 1 | 2 | 3 | 4;

export const FSRS_RATING: { Again: FsrsRating; Hard: FsrsRating; Good: FsrsRating; Easy: FsrsRating } = {
    Again: 1,
    Hard: 2,
    Good: 3,
    Easy: 4,
};


const REVIEW_SOURCE_EXPLICIT_RATING = 3;

export interface WordProgress {
    id: string;
    wordId: string;
    term: string;
    reading: string | null;
    meaning: string;
    stability: number;
    stabilityFast: number;
    difficulty: number;
    dueAt: string;
    lastReviewedAt: string | null;
    reviewCount: number;
    lapseCount: number;
}

const API_BASE_URL = "http://localhost:5166";

export async function getDueWords(userId: string): Promise<WordProgress[]> {
    const response = await fetch(`${API_BASE_URL}/api/users/${userId}/word-progress/due`, {
        headers: getAuthHeaders(),
    });
    if (!response.ok) {
        throw new Error(`Failed to load due words: ${response.status}`);
    }
    return response.json();
}

export async function submitReview(userId: string, wordId: string, rating: FsrsRating): Promise<WordProgress> {
    const response = await fetch(`${API_BASE_URL}/api/users/${userId}/word-progress/reviews`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...getAuthHeaders(),
        },
        body: JSON.stringify({
            wordId,
            eventId: crypto.randomUUID(),
            rating,
            source: REVIEW_SOURCE_EXPLICIT_RATING,
        }),
    });
    if (!response.ok) {
        throw new Error(`Failed to submit review: ${response.status}`);
    }
    return response.json();
}