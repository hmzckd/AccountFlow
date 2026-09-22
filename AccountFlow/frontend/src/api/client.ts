// src/api/client.ts
import type { RefreshTokenRequestDto, TokenResponseDto } from "../types/auth";

const API_BASE =
    import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5205/api";

// In-memory storage avoids repeated Web Storage access during requests.
let accessToken: string | null = null;
let refreshToken: string | null = null;
let storageMode: "local" | "session" = "local";
let refreshPromise: Promise<boolean> | null = null;
const tokensChangedEvent = "accountflow:tokens-changed";
const accessTokenKey = "accessToken";
const refreshTokenKey = "refreshToken";

function readTokenPair(storage: Storage): TokenResponseDto | null {
    const storedAccessToken = storage.getItem(accessTokenKey);
    const storedRefreshToken = storage.getItem(refreshTokenKey);

    return storedAccessToken && storedRefreshToken
        ? { accessToken: storedAccessToken, refreshToken: storedRefreshToken }
        : null;
}

// Session storage belongs to this tab, so it takes precedence over a remembered
// login created by another tab. Incomplete token pairs are treated as logged out.
function synchronizeTokensFromStorage() {
    const sessionTokens = readTokenPair(window.sessionStorage);
    const localTokens = readTokenPair(window.localStorage);
    const storedTokens = sessionTokens ?? localTokens;

    accessToken = storedTokens?.accessToken ?? null;
    refreshToken = storedTokens?.refreshToken ?? null;
    storageMode = sessionTokens ? "session" : "local";
}

function notifyTokenChanges() {
    window.dispatchEvent(new Event(tokensChangedEvent));
}

// Restore authentication state before React renders protected routes.
synchronizeTokensFromStorage();

// The custom event below only reaches the current document. Web Storage emits a
// separate event in the other tabs, so keep their in-memory state in sync too.
window.addEventListener("storage", (event) => {
    if (
        event.storageArea !== window.localStorage ||
        (event.key !== null &&
            event.key !== accessTokenKey &&
            event.key !== refreshTokenKey)
    ) {
        return;
    }

    synchronizeTokensFromStorage();
    notifyTokenChanges();
});

/**
 * Updates authentication tokens in memory and the selected browser storage.
 * Pass null to clear tokens (logout).
 */
export function getCurrentTokens(): TokenResponseDto | null {
    return accessToken && refreshToken ? { accessToken, refreshToken } : null;
}

export function subscribeToTokenChanges(listener: () => void): () => void {
    window.addEventListener(tokensChangedEvent, listener);
    return () => window.removeEventListener(tokensChangedEvent, listener);
}

export function setTokens(
    tokens: TokenResponseDto | null,
    mode: "local" | "session" = storageMode
) {
    if (!tokens) {
        accessToken = null;
        refreshToken = null;
        for (const storage of [window.localStorage, window.sessionStorage]) {
            storage.removeItem(accessTokenKey);
            storage.removeItem(refreshTokenKey);
        }
        notifyTokenChanges();
        return;
    }

    accessToken = tokens.accessToken;
    refreshToken = tokens.refreshToken;
    storageMode = mode;

    const selectedStorage = mode === "local" ? window.localStorage : window.sessionStorage;
    const otherStorage = mode === "local" ? window.sessionStorage : window.localStorage;
    otherStorage.removeItem(accessTokenKey);
    otherStorage.removeItem(refreshTokenKey);
    selectedStorage.setItem(accessTokenKey, tokens.accessToken);
    selectedStorage.setItem(refreshTokenKey, tokens.refreshToken);
    notifyTokenChanges();
}

/**
 * Attempts to renew the access token using the current refresh token.
 * Returns true if successful, otherwise clears tokens (logout) and returns false.
 */
async function requestRefresh(): Promise<boolean> {
    const tokenBeingRefreshed = refreshToken;
    const storageModeBeingRefreshed = storageMode;
    if (!tokenBeingRefreshed) return false;

    const body: RefreshTokenRequestDto = {
        refreshToken: tokenBeingRefreshed,
    };

    const res = await fetch(`${API_BASE}/auth/refresh`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
    });

    const sessionHasNotChanged = () => {
        const persistedRefreshToken = (
            storageModeBeingRefreshed === "local"
                ? window.localStorage
                : window.sessionStorage
        ).getItem(refreshTokenKey);

        return (
            storageMode === storageModeBeingRefreshed &&
            refreshToken === tokenBeingRefreshed &&
            persistedRefreshToken === tokenBeingRefreshed
        );
    };

    if (!res.ok) {
        if (sessionHasNotChanged()) {
            setTokens(null);
        } else {
            synchronizeTokensFromStorage();
            notifyTokenChanges();
        }
        return false;
    }

    const data = (await res.json()) as TokenResponseDto;
    if (!sessionHasNotChanged()) {
        synchronizeTokensFromStorage();
        notifyTokenChanges();
        return false;
    }
    setTokens(data);
    return true;
}

async function doRefresh(): Promise<boolean> {
    if (!refreshPromise) {
        refreshPromise = requestRefresh().finally(() => {
            refreshPromise = null;
        });
    }

    return refreshPromise;
}

/**
 * Turns a failed response into an Error carrying a human-readable message.
 * Understands the backend's shapes: { message }, { errors: [...] }, a bare array, or plain text.
 */
async function toError(res: Response): Promise<Error> {
    const raw = await res.text();
    if (raw) {
        try {
            const data = JSON.parse(raw);
            if (data && typeof data.message === "string") return new Error(data.message);
            if (Array.isArray(data?.errors)) return new Error(data.errors.join("\n"));
            if (Array.isArray(data)) return new Error(data.join("\n"));
        } catch {
            // Body wasn't JSON; fall back to the raw text below.
        }
        return new Error(raw);
    }
    return new Error(`Request failed with status ${res.status}`);
}

/**
 * Generic fetch wrapper that handles headers, authentication injection,
 * and automatic token refreshing mechanism on 401 Unauthorized errors.
 */
async function rawRequest<T>(
    path: string,
    options: RequestInit = {}
): Promise<T> {
    const send = () => fetch(`${API_BASE}${path}`, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
            ...(options.headers || {}),
        },
    });

    let res = await send();

    if (res.status === 401) {
        const refreshed = await doRefresh();
        if (refreshed) {
            res = await send();
        }
    }

    if (!res.ok) throw await toError(res);
    if (res.status === 204) return null as T;
    return (await res.json()) as T;
}

/**
 * Typed HTTP client methods for communicating with the backend API.
 */
export const api = {
    get: <T>(path: string) =>
        rawRequest<T>(path, {
            method: "GET",
        }),
    post: <T>(path: string, body?: unknown) =>
        rawRequest<T>(path, {
            method: "POST",
            body: body ? JSON.stringify(body) : undefined,
        }),
};
