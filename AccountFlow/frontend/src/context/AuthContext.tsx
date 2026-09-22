import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { login as loginRequest } from "../api/auth";
import type { LoginDto, TokenResponseDto } from "../types/auth";
import { getCurrentTokens, setTokens as setTokensInClient, subscribeToTokenChanges } from "../api/client";
import { AuthContext } from "./AuthContextObject";

export type LoginResult = {
    role: string | null;
};

export type AuthContextType = {
    isAuthenticated: boolean;
    role: string | null;
    tokens: TokenResponseDto | null;
    login: (creds: LoginDto, remember: boolean) => Promise<LoginResult>;
    logout: () => void;
};

// Reads the role claim out of the JWT access token (no signature check — the backend still
// enforces authorization; this only drives what the UI shows).
function decodeRole(accessToken: string | undefined): string | null {
    if (!accessToken) return null;
    try {
        const part = accessToken.split(".")[1];
        let b64 = part.replace(/-/g, "+").replace(/_/g, "/");
        b64 += "===".slice((b64.length + 3) % 4); // restore base64 padding
        const claims = JSON.parse(atob(b64)) as Record<string, unknown>;
        const role =
            claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
            claims["role"] ??
            claims["Role"];
        return typeof role === "string" ? role : null;
    } catch {
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    // Read synchronously on the very first render. If we hydrated inside useEffect instead,
    // the first render would be logged-out and ProtectedRoute would redirect to /login
    // on every page refresh before hydration could run.
    const [tokens, setTokens] = useState<TokenResponseDto | null>(getCurrentTokens);

    useEffect(() => subscribeToTokenChanges(() => setTokens(getCurrentTokens())), []);

    async function login(creds: LoginDto, remember: boolean): Promise<LoginResult> {
        const data = await loginRequest(creds);
        setTokensInClient(data, remember ? "local" : "session");
        setTokens(data);
        return { role: decodeRole(data.accessToken) };
    }

    function logout() {
        setTokens(null);
        setTokensInClient(null);
    }

    return (
        <AuthContext.Provider
            value={{
                isAuthenticated: !!tokens?.accessToken,
                role: decodeRole(tokens?.accessToken),
                tokens,
                login,
                logout,
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}
