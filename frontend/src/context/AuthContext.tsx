import { createContext, useState } from "react";
import type { User } from "../api/auth";


export interface AuthContextType {
    token: string | null;
    login: (token: string, user: User) => void;
    logout: () => void;
    user: User | null;
}

export const AuthContext = createContext<AuthContextType>({ token: null, login: () => { }, logout: () => { }, user: null });



export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(() => localStorage.getItem("authToken"));
    function login(newToken: string, newUser: User) {
        localStorage.setItem("authToken", newToken);
        setToken(newToken);
        setUser(newUser);
    }
    function logout() {
        localStorage.removeItem("authToken");
        setToken(null);
    }
    return <AuthContext.Provider value={{ token, login, logout,  user}}>{children}</AuthContext.Provider>;
}
