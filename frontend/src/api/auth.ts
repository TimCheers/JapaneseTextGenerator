export interface RegisterRequest {
    displayName: string;
    email: string;
    password: string;
    nativeLanguage: string | null;
}
export interface LoginRequest {
    email: string;
    password: string;
}
export interface User {
    id: string;
    displayName: string;
    email: string;
    nativeLanguage: string | null;
    createdAt: string;
}
export interface AuthResponse {
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

export async function loginUser(data: LoginRequest): Promise<AuthResponse> {
    const response = await fetch("http://localhost:5166/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
    })
    if (!response.ok) {
        throw new Error(`Unauthorized: ${response.status}`);
    }

    return response.json();
}