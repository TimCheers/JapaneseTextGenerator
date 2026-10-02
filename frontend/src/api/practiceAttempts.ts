import { getAuthHeaders } from "./authHeaders"
import { API_BASE_URL } from "./config"


export interface PracticeAttempt {
    id: string;
    generatedTextId: string;
    startedAt: string;
    completedAt: string | null;
    score: number | null;
}


export async function createPracticeAttempt(userId: string, generatedTextId: string): Promise<PracticeAttempt> {
    const response = await fetch(
        `${API_BASE_URL}/api/users/${userId}/practice-attempts?generatedTextId=${generatedTextId}`,
        {
            method: "POST",
            headers: getAuthHeaders(),
        }
    );
    if (!response.ok) {
        throw new Error(`Failed to start practice attempt: ${response.status}`);
    }
    return response.json();
}

export async function completePracticeAttempt(userId: string, attemptId: string): Promise<PracticeAttempt> {
    const response = await fetch(`${API_BASE_URL}/api/users/${userId}/practice-attempts/${attemptId}/complete`, {
        method: "PUT",
        headers: getAuthHeaders(),
    });
    if (!response.ok) {
        throw new Error(`Failed to complete practice attempt: ${response.status}`);
    }
    return response.json();
}