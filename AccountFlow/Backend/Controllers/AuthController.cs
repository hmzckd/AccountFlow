using AccountFlow.Backend.Common;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace AccountFlow.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("auth")] // Brute-force protection on all auth endpoints

    // Handles user authentication, registration, and account management operations
    public class AuthController(
        IAuthService authService,
        IValidator<RegisterDto> registerValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordRequestValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<ResendVerificationRequestDto> resendVerificationValidator

        ) : ControllerBase
    {
        private readonly IValidator<RegisterDto> _registerValidator = registerValidator;
        private readonly IValidator<ResetPasswordRequestDto> _resetPasswordRequestValidator = resetPasswordRequestValidator;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator = resetPasswordValidator;
        private readonly IAuthService _authService = authService;
        private readonly IValidator<LoginDto> _loginValidator = loginValidator;
        private readonly IValidator<ResendVerificationRequestDto> _resendVerificationValidator = resendVerificationValidator;


        // Validates input data, registers a new user, and triggers the verification email process
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto request)
        {
            ValidationResult result = _registerValidator.Validate(request);
            if (!result.IsValid)
                return BadRequest(new { errors = result.Errors.Select(e => e.ErrorMessage) });

            var registration = await _authService.RegisterAsync(request);
            return registration.Status switch
            {
                RegisterStatus.Success => Ok(new { message = "Successfully registered. Please check your inbox to confirm your account." }),
                RegisterStatus.EmailAlreadyExists => Conflict(new { message = "Email already exists." }),
                RegisterStatus.EmailSendFailed => StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Could not send the verification email. Please try again later." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected error." })
            };
        }
        // Authenticates user credentials and returns JWT access and refresh tokens
        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto request)
        {
            ValidationResult valResult = _loginValidator.Validate(request);
            if (!valResult.IsValid)
                return BadRequest(new { errors = valResult.Errors.Select(e => e.ErrorMessage) });

            var result = await _authService.LoginAsync(request);
            if (result == null)
                return BadRequest(new { message = "Invalid credentials or email not verified." });

            return Ok(result);
        }
        // Initiates the password recovery process by generating a token and sending an email
        [HttpPost("password/reset-request")]
        public async Task<ActionResult<string>> RequestResetPassword(ResetPasswordRequestDto dto)
        {
            ValidationResult result = _resetPasswordRequestValidator.Validate(dto);
            if (!result.IsValid)
                return BadRequest(new { errors = result.Errors.Select(e => e.ErrorMessage) });

            // Always return the same response regardless of whether the email exists,
            // so this endpoint can't be used to enumerate registered accounts.
            await _authService.GeneratePasswordResetToken(dto.Email);

            return Ok(new { success = true, message = "If the email exists, a confirmation code has been generated and sent." });
        }
        // Resets the user's password using a valid token and the new password provided
        [HttpPost("password/reset")]
        public async Task<ActionResult<string>> ResetPassword(ResetPasswordDto dto)
        {
            ValidationResult result = _resetPasswordValidator.Validate(dto);
            if (!result.IsValid)
                return BadRequest(new { errors = result.Errors.Select(e => e.ErrorMessage) });

            var user = await _authService.ResetPasswordAsync(dto);
            if (user == null)
                return BadRequest(new { message = "Invalid or expired token." });

            return Ok(new { message = "Password has been reset successfully." });
        }
        // Verifies the user's email address using the token sent during registration
        [HttpPost("verify-email")]
        public async Task<ActionResult> Verify(VerifyDto request)
        {
            // The token is validated against its stored hash inside the service; no extra shape check here.
            var user = await _authService.VerifyAsync(request.Token);
            if (user == null) return BadRequest(new { message = "Invalid or expired verification token." });

            return Ok(new { message = "Email verified successfully." });
        }

        [HttpPost("verify-email/resend")]
        public async Task<ActionResult> ResendVerification(ResendVerificationRequestDto request)
        {
            ValidationResult validation = _resendVerificationValidator.Validate(request);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors.Select(error => error.ErrorMessage) });

            await _authService.ResendVerificationAsync(request.Email);
            return Ok(new { message = "If this address belongs to an unverified account, check your inbox for a verification link." });
        }

        // Generates a new access token using a valid refresh token
        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Invalid refresh token." });
            return Ok(result);
        }

    }
}
