import { getAuthHeaders } from "./authHeaders"

export interface ComprehensionQuestion {
    id: string;
    generatedTextId: string;
    questionText: string;
    options: string[];
}

interface RawComprehensionQuestion {
    id: string;
    generatedTextId: string;
    questionText: string;
    options: string;
}

const API_BASE_URL = "http://localhost:5166";

export async function getComprehensionQuestions(
    userId: string,
    generatedTextId: string
): Promise<ComprehensionQuestion[]> {
    const response = await fetch(
        `${API_BASE_URL}/api/users/${userId}/generated-texts/${generatedTextId}/questions`,
        { headers: getAuthHeaders() }
    );
    if (!response.ok) {
        throw new Error(`Failed to load questions: ${response.status}`);
    }

    const raw: RawComprehensionQuestion[] = await response.json();
    return raw.map((q) => ({
        id: q.id,
        generatedTextId: q.generatedTextId,
        questionText: q.questionText,
        options: JSON.parse(q.options) as string[],
    }));
}