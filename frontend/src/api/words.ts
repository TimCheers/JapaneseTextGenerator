import { getAuthHeaders } from "./authHeaders"

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
    deckId: string;
    term: string;
    reading: string;
    meaning: string;
    partOfSpeech: string | null;
    jlptLevel: string | null;
    exampleSentence: string;
    notes: string | null;
    acquisitionSource: string | null;
}

const API_BASE_URL = "http://localhost:5166";

export async function getWordsForDeck(deckId: string): Promise<Word[]>{
      const response = await fetch(`${API_BASE_URL}/api/decks/${deckId}/words`, { headers: getAuthHeaders(), });
    
      if (!response.ok) {
        throw new Error(`Failed to load words: ${response.status}`);
      }
    
      return response.json();
}