export interface RegisterRequest {
    displayName: string;
    email: string;
    password: string;
    nativeLanguage: string | null;
}
interface User {
    id: string;
    displayName: string;
    email: string;
    nativeLanguage: string | null;
    createdAt: string;
}
interface AuthResponse {
    authToken: string;
    user: User;
}

export async function registerUser(data: RegisterRequest): Promise<AuthResponse> {
    const response = await fetch("http://localhost:5166/api/Users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
    })
    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    return response.json();
}