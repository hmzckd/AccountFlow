using AccountFlow.Backend.Common;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;

namespace AccountFlow.Backend.Services
{
    // Defines the contract for authentication, user management, and token operations
    public interface IAuthService
    {
        // Registers a new user and triggers the email verification process.
        // Returns a status so the caller can respond accurately for each failure reason.
        Task<RegisterResult> RegisterAsync(RegisterDto request);

        // Authenticates credentials and issues access/refresh tokens
        Task<TokenResponseDto?> LoginAsync(LoginDto request);

        // Generates and sends a password reset token via email.
        // Intentionally returns no result so callers can't leak whether the email exists.
        Task GeneratePasswordResetToken(string email);

        // Resets the password using a valid recovery token
        Task<User?> ResetPasswordAsync(ResetPasswordDto dto);

        // Verifies the user's email address using a token
        Task<User?> VerifyAsync(string token);

        // Sends a fresh verification link when an unverified account is eligible.
        Task ResendVerificationAsync(string email);

        // Counts users registered in the last 24 hours
        Task<long> GetDailyRegistrationCountAsync();

        // Counts unverified users registered in the last 24 hours
        Task<long> GetDailyUnverifiedCountAsync();

        // Returns dashboard counts in one aggregation query
        Task<AdminStatsDto> GetDailyStatsAsync();

        // Validates a refresh token and issues a new token pair
        Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
    }
}
