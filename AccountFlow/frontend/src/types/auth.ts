// src/types/auth.ts

// matches LoginDto
export type LoginDto = {
    email: string;
    password: string;
};

// matches RegisterDto
export type RegisterDto = {
    name: string;
    surname: string;
    email: string;
    password: string;
};

// matches VerifyDto
export type VerifyDto = {
    token: string;
};

export type ResendVerificationRequestDto = {
    email: string;
};

// matches ResetPasswordRequestDto
export type ResetPasswordRequestDto = {
    email: string;
};

// matches ResetPasswordDto
export type ResetPasswordDto = {
    newPassword: string;
    token: string;
};

// matches RefreshTokenRequestDto
export type RefreshTokenRequestDto = {
    refreshToken: string;
};

// matches TokenResponseDto
export type TokenResponseDto = {
    accessToken: string;
    refreshToken: string;
};

export type MessageResponse = {
    message: string;
};
