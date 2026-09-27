import { getAuthHeaders } from "./authHeaders"


const API_BASE_URL = "http://localhost:5166";

export async function createGenerationRequest(userId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/users/${userId}/generation-requests`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeaders(),
    },
    body: JSON.stringify({ promptParams: null }),
  });
  
  const result = await response.json();
  if (result.status === "Failed") {
    throw new Error(result.errorMessage ?? "Text generation failed");
  }

  if (!response.ok) {
    throw new Error(`Failed to create generation request: ${response.status}`);
  }
}