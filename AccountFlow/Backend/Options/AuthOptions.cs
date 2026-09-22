using System.ComponentModel.DataAnnotations;

namespace AccountFlow.Backend.Options
{
    // Centralizes token lifetimes and code lengths so they aren't scattered as magic numbers.
    public class AuthOptions
    {
        public const string SectionKey = "Auth";

        [Range(1, 1440, ErrorMessage = "AccessTokenMinutes must be between 1 and 1440.")]
        public int AccessTokenMinutes { get; set; } = 15;

        [Range(1, 365, ErrorMessage = "RefreshTokenDays must be between 1 and 365.")]
        public int RefreshTokenDays { get; set; } = 7;

        [Range(1, 1440, ErrorMessage = "VerificationCodeMinutes must be between 1 and 1440.")]
        public int VerificationCodeMinutes { get; set; } = 15;

        [Range(1, 1440, ErrorMessage = "ResetCodeMinutes must be between 1 and 1440.")]
        public int ResetCodeMinutes { get; set; } = 15;

        [Range(6, 64, ErrorMessage = "VerificationCodeLength must be between 6 and 64.")]
        public int VerificationCodeLength { get; set; } = 8;

        [Range(32, 256, ErrorMessage = "RefreshTokenBytes must be between 32 and 256.")]
        public int RefreshTokenBytes { get; set; } = 32;

        // When false (e.g. local dev without SMTP), the verification/reset link is written to the
        // logs instead of being emailed, so the full flow stays testable without a mail server.
        public bool SendVerificationEmails { get; set; } = true;
    }
}
