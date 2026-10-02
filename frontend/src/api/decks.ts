import { getAuthHeaders } from "./authHeaders"
import { API_BASE_URL } from "./config"


export interface Deck {
  id: string;
  userId: string;
  name: string;
  description: string | null;
  createdAt: string;
}
export interface CreateDeckRequest {
  name: string;
  description: string | null;
}

export async function getDecks(userId: string): Promise<Deck[]> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks`, { headers: getAuthHeaders(), });

  if (!response.ok) {
    throw new Error(`Failed to load decks: ${response.status}`);
  }

  return response.json();
}

export async function getDeckById(userId: string, deckId: string): Promise<Deck> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks/${deckId}`, { headers: getAuthHeaders(), });

  if (!response.ok) {
    throw new Error(`Failed to load decks: ${response.status}`);
  }

  return response.json();
}

export async function createDeck(userId: string, data: CreateDeckRequest): Promise<Deck> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks`, {
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

export async function updateDeck(userId: string, deckId: string, data: CreateDeckRequest): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks/${deckId}`, {
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

export async function deleteDeck(userId: string, deckId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks/${deckId}`, {
    method: "DELETE",
    headers: getAuthHeaders()
  }
  )
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
}