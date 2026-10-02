import { getAuthHeaders } from "./authHeaders"
import { API_BASE_URL } from "./config"


export interface Word {
    id: string;
    deckId: string;
    term: string;
    reading: string;
    meaning: string;
    partOfSpeech: string | null;
    jlptLevel: string | null;
    exampleSentence: string;
    notes: string | null;
    acquisitionSource: string | null;
    createdAt: string;
}

export interface CreateWordRequest {
    term: string;
    reading: string;
    meaning: string;
    partOfSpeech: string | null;
    jlptLevel: string | null;
    exampleSentence: string;
    notes: string | null;
    acquisitionSource: string | null;
}


export async function getWordsForDeck(deckId: string): Promise<Word[]> {
    const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words`, { headers: getAuthHeaders(), });

    if (!response.ok) {
        throw new Error(`Failed to load words: ${response.status}`);
    }

    return response.json();
}

export async function importWordsFromExcel(deckId: string, file: File): Promise<Word[]> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words/import`, {
        method: "POST",
        headers: getAuthHeaders(),
        body: formData,
    });

    if (!response.ok) {
        throw new Error(`Failed to create generation request: ${response.status}`);
    }

    return response.json();
}

export async function createWord(deckId: string, data: CreateWordRequest): Promise<Word> {
    const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...getAuthHeaders(),
        },
        body: JSON.stringify(data),
    })
    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    return response.json();
}

export async function updateWord(deckId: string, wordId: string, data: CreateWordRequest): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words/${wordId}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            ...getAuthHeaders(),
        },
        body: JSON.stringify(data),
    })
    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }
}

export async function deleteWord(deckId: string, wordId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words/${wordId}`, {
        method: "DELETE",
        headers: getAuthHeaders()
    }
    )
    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }
}