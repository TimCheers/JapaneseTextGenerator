export interface GeneratedText {
    id: string;
    generationRequestId: string;
    content: string;
    createdAt: string;
}

const API_BASE_URL = "http://localhost:5166";


export async function getGeneratedTexts(userId: string): Promise<GeneratedText[]> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/generated-texts`);

  if (!response.ok) {
    throw new Error(`Failed to load texts: ${response.status}`);
  }

  return response.json();
}