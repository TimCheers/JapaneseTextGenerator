import { getAuthHeaders } from "./authHeaders"
import { API_BASE_URL } from "./config"


export interface UserAnswer {
    id: string;
    comprehensionQuestionId: string;
    selectedAnswer: string;
    isCorrect: boolean;
    answeredAt: string;
}


export async function submitAnswer(
    userId: string,
    practiceAttemptId: string,
    comprehensionQuestionId: string,
    selectedAnswer: string
): Promise<UserAnswer> {
    const response = await fetch(
        `${API_BASE_URL}/api/users/${userId}/practice-attempts/${practiceAttemptId}/answers`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                ...getAuthHeaders(),
            },
            body: JSON.stringify({ comprehensionQuestionId, selectedAnswer }),
        }
    );
    if (!response.ok) {
        throw new Error(`Failed to submit answer: ${response.status}`);
    }
    return response.json();
}