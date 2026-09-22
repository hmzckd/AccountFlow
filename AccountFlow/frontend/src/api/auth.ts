// src/api/auth.ts
import { api, setTokens } from "./client";
import type {
    LoginDto,
    RegisterDto,
    VerifyDto,
    ResendVerificationRequestDto,
    ResetPasswordRequestDto,
    ResetPasswordDto,
    RefreshTokenRequestDto,
    TokenResponseDto,
    MessageResponse,
} from "../types/auth";

/**
 * Authenticates the user and returns the received access/refresh tokens.
 */
export async function login(body: LoginDto): Promise<TokenResponseDto> {
    return api.post<TokenResponseDto>("/auth/login", body);
}

/**
 * Registers a new user account and triggers the verification email.
 */
export async function register(body: RegisterDto): Promise<MessageResponse> {
    return api.post<MessageResponse>("/auth/register", body);
}

/**
 * Verifies the user's email address using the provided token.
 */
export async function verifyEmail(body: VerifyDto): Promise<MessageResponse> {
    return api.post<MessageResponse>("/auth/verify-email", body);
}

export async function resendVerificationEmail(
    body: ResendVerificationRequestDto
): Promise<MessageResponse> {
    return api.post<MessageResponse>("/auth/verify-email/resend", body);
}

/**
 * Initiates the password recovery process by sending a reset link to the email.
 */
export async function requestPasswordReset(
    body: ResetPasswordRequestDto
): Promise<MessageResponse> {
    return api.post<MessageResponse>("/auth/password/reset-request", body);
}

/**
 * Resets the user's password using a valid recovery token.
 */
export async function resetPassword(
    body: ResetPasswordDto
): Promise<void> {
    return api.post<void>("/auth/password/reset", body);
}

/**
 * Refreshes the access token using a valid refresh token.
 */
export async function refreshTokens(
    body: RefreshTokenRequestDto
): Promise<TokenResponseDto> {
    const data = await api.post<TokenResponseDto>("/auth/refresh", body);
    setTokens(data);
    return data;
}
