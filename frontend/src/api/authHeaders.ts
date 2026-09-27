export function getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem("authToken");
    return {
        Authorization: `Bearer ${token}`,
    };
}