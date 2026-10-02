import { getAuthHeaders } from "./authHeaders"
import { API_BASE_URL } from "./config"



export interface GeneratedText {
    id: string;
    generationRequestId: string;
    content: string;
    createdAt: string;
}

export async function getGeneratedTexts(userId: string): Promise<GeneratedText[]> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/generated-texts` , { headers: getAuthHeaders(), });

  if (!response.ok) {
    throw new Error(`Failed to load texts: ${response.status}`);
  }

  return response.json();
}