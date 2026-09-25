import { createContext, useState } from "react";


export interface AuthContextType {
    token: string | null;
    login: (token: string) => void;
    logout: () => void;
}

export const AuthContext = createContext<AuthContextType>({ token: null, login: () => { }, logout: () => { } });



export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [token, setToken] = useState<string | null>(() => localStorage.getItem("authToken"));
    function login(newToken: string) {
        localStorage.setItem("authToken", newToken);
        setToken(newToken);
    }
    function logout() {
        localStorage.removeItem("authToken");
        setToken(null);
    }
    return <AuthContext.Provider value={{ token, login, logout }}>{children}</AuthContext.Provider>;
}
