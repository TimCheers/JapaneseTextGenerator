export interface Deck {
  id: string;
  userId: string;
  name: string;
  description: string | null;
  createdAt: string;
}

const API_BASE_URL = "http://localhost:5166";

export async function getDecks(userId: string): Promise<Deck[]> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/decks`);

  if (!response.ok) {
    throw new Error(`Failed to load decks: ${response.status}`);
  }

  return response.json() as Promise<Deck[]>;
}