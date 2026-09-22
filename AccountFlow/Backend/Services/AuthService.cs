using AccountFlow.Backend.Common;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AccountFlow.Backend.Services
{
    // Handles all authentication logic including registration, login, token management, and password recovery
    public class AuthService(
        MongoDbService mongoDbService,
        IEmailService emailSender,
        IConfiguration configuration,
        IOptions<AuthOptions> authOptions,
        ILogger<AuthService> logger) : IAuthService
    {
        private static readonly Collation EmailCollation = new("en", strength: CollationStrength.Secondary);
        private static readonly TimeSpan VerificationResendCooldown = TimeSpan.FromMinutes(1);
        private readonly IMongoCollection<User> _users = mongoDbService.Users;
        private readonly IEmailService _emailSender = emailSender;
        private readonly IConfiguration _configuration = configuration;
        private readonly AuthOptions _auth = authOptions.Value;
        private readonly ILogger<AuthService> _logger = logger;

        // Registers a new user and saves them to the database, then delivers a verification link.
        // If email delivery is enabled and fails, the user record is rolled back.
        public async Task<RegisterResult> RegisterAsync(RegisterDto request)
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            var existingUser = await FindByEmail(normalizedEmail).FirstOrDefaultAsync();
            if (existingUser is not null)
            {
                return new RegisterResult(RegisterStatus.EmailAlreadyExists);
            }

            var user = new User
            {
                Email = normalizedEmail,
                Name = request.Name,
                Surname = request.Surname,
                IsVerified = false
            };

            var hasher = new PasswordHasher<User>();
            user.HashedPassword = hasher.HashPassword(user, request.Password);

            var plainToken = GenerateAlphaNumericCode(_auth.VerificationCodeLength);
            user.HashedVerificationCode = ComputeSha256(plainToken);
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(_auth.VerificationCodeMinutes);
            user.LastVerificationEmailSentAt = DateTime.UtcNow;

            try
            {
                await _users.InsertOneAsync(user);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Lost the race against another concurrent registration for the same email.
                return new RegisterResult(RegisterStatus.EmailAlreadyExists);
            }

            try
            {
                await SendVerificationEmailAsync(user, plainToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}; rolling back registration.", user.Email);
                var deleteFilter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                await _users.DeleteOneAsync(deleteFilter);
                return new RegisterResult(RegisterStatus.EmailSendFailed);
            }

            return new RegisterResult(RegisterStatus.Success, user);
        }

        // Authenticates a user and returns a token pair if credentials are valid and email is verified
        public async Task<TokenResponseDto?> LoginAsync(LoginDto request)
        {
            var user = await FindByEmail(request.Email).FirstOrDefaultAsync();
            if (user == null) return null;
            if (!user.IsVerified) return null;

            var verify = new PasswordHasher<User>().VerifyHashedPassword(user, user.HashedPassword, request.Password);
            if (verify == PasswordVerificationResult.Failed) return null;
            return await CreateTokenResponse(user);
        }

        // Generates a password reset token for the specified email and sends it via email.
        // Returns void-like semantics on purpose: callers must not leak whether the email exists.
        public async Task GeneratePasswordResetToken(string email)
        {
            var user = await FindByEmail(email).FirstOrDefaultAsync();
            if (user == null || user.IsVerified == false) return;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var hashedToken = ComputeSha256(token);

            var expiry = DateTime.UtcNow.AddMinutes(_auth.ResetCodeMinutes);
            var update = Builders<User>.Update
                .Set(u => u.HashedResetCode, hashedToken)
                .Set(u => u.ResetCodeExpiry, expiry);

            await _users.UpdateOneAsync(u => u.Id == user.Id, update);

            var resetLink = $"{GetFrontendUrl()}/reset?token={token}";

            if (!_auth.SendVerificationEmails)
            {
                // Dev / no-SMTP mode: surface the link in the logs instead of emailing it.
                _logger.LogInformation("Email sending disabled. Password reset link for {Email}: {Link}", user.Email, resetLink);
                return;
            }

            try
            {
                await _emailSender.SendMailAsync(new EmailDto
                {
                    To = user.Email,
                    Subject = "Reset Your Password",
                    Body = $@"
                        You requested a password reset.

                        Click the link below to set a new password (valid for {_auth.ResetCodeMinutes} minutes):
                        {resetLink}

                        If you didn't request this, you can ignore this email.
    "
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}.", user.Email);

                try
                {
                    var clearFilter = Builders<User>.Filter.And(
                        Builders<User>.Filter.Eq(u => u.Id, user.Id),
                        Builders<User>.Filter.Eq(u => u.HashedResetCode, hashedToken));
                    var clearUpdate = Builders<User>.Update
                        .Set(u => u.HashedResetCode, null)
                        .Set(u => u.ResetCodeExpiry, null);
                    await _users.UpdateOneAsync(clearFilter, clearUpdate);
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Failed to clear an undelivered password reset token for {Email}.",
                        user.Email);
                }
            }
        }

        // Resets the user's password using a valid reset token and invalidates any active sessions
        public async Task<User?> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var token = ComputeSha256(dto.Token);
            var user = await _users.Find(u => u.HashedResetCode == token && u.ResetCodeExpiry >= DateTime.UtcNow).FirstOrDefaultAsync();
            if (user == null || user.IsVerified == false) return null;

            var hasher = new PasswordHasher<User>();
            var newHashed = hasher.HashPassword(user, dto.NewPassword);

            // Clearing the refresh token forces all existing sessions to re-authenticate after a reset.
            var update = Builders<User>.Update
                .Set(u => u.HashedPassword, newHashed)
                .Set(u => u.HashedResetCode, null)
                .Set(u => u.ResetCodeExpiry, null)
                .Set(u => u.HashedRefreshToken, null)
                .Inc(u => u.SessionVersion, 1);

            // Match the still-valid token in the update itself. Concurrent requests may both
            // read it above, but only one can consume it and change the password.
            var now = DateTime.UtcNow;
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                Builders<User>.Filter.Eq(u => u.IsVerified, true),
                Builders<User>.Filter.Eq(u => u.HashedResetCode, token),
                Builders<User>.Filter.Gte(u => u.ResetCodeExpiry, now));

            return await _users.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });
        }

        // Verifies the user's account using the token sent during registration
        public async Task<User?> VerifyAsync(string token)
        {
            var hashedToken = ComputeSha256(token);
            var now = DateTime.UtcNow;
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.HashedVerificationCode, hashedToken),
                Builders<User>.Filter.Gte(u => u.VerificationCodeExpiry, now),
                Builders<User>.Filter.Eq(u => u.IsVerified, false));
            var update = Builders<User>.Update
                .Set(u => u.IsVerified, true)
                .Set(u => u.HashedVerificationCode, null)
                .Set(u => u.VerificationCodeExpiry, null);

            return await _users.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });
        }

        public async Task ResendVerificationAsync(string email)
        {
            var user = await FindByEmail(email).FirstOrDefaultAsync();
            if (user is null || user.IsVerified) return;

            var plainToken = GenerateAlphaNumericCode(_auth.VerificationCodeLength);
            var hashedToken = ComputeSha256(plainToken);
            var now = DateTime.UtcNow;
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                Builders<User>.Filter.Eq(u => u.IsVerified, false),
                Builders<User>.Filter.Or(
                    Builders<User>.Filter.Eq(u => u.LastVerificationEmailSentAt, null),
                    Builders<User>.Filter.Lte(u => u.LastVerificationEmailSentAt, now - VerificationResendCooldown)));
            var update = Builders<User>.Update
                .Set(u => u.HashedVerificationCode, hashedToken)
                .Set(u => u.VerificationCodeExpiry, now.AddMinutes(_auth.VerificationCodeMinutes))
                .Set(u => u.LastVerificationEmailSentAt, now);

            // The per-account cooldown is checked and updated atomically so concurrent requests
            // cannot trigger multiple messages or invalidate each other's new links.
            var previous = await _users.FindOneAndUpdateAsync(filter, update);
            if (previous is null) return;

            try
            {
                await SendVerificationEmailAsync(previous, plainToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email to {Email}.", previous.Email);
                try
                {
                    var rollbackFilter = Builders<User>.Filter.And(
                        Builders<User>.Filter.Eq(u => u.Id, previous.Id),
                        Builders<User>.Filter.Eq(u => u.HashedVerificationCode, hashedToken),
                        Builders<User>.Filter.Eq(u => u.IsVerified, false));
                    var rollback = Builders<User>.Update
                        .Set(u => u.HashedVerificationCode, previous.HashedVerificationCode)
                        .Set(u => u.VerificationCodeExpiry, previous.VerificationCodeExpiry)
                        .Set(u => u.LastVerificationEmailSentAt, previous.LastVerificationEmailSentAt);
                    await _users.UpdateOneAsync(rollbackFilter, rollback);
                }
                catch (Exception rollbackException)
                {
                    _logger.LogError(rollbackException,
                        "Failed to restore verification token after undelivered email for {Email}.",
                        previous.Email);
                }
            }
        }

        private async Task SendVerificationEmailAsync(User user, string plainToken)
        {
            var verificationLink = $"{GetFrontendUrl()}/verify?token={plainToken}";
            if (!_auth.SendVerificationEmails)
            {
                _logger.LogInformation("Email sending disabled. Verification link for {Email}: {Link}", user.Email, verificationLink);
                return;
            }

            await _emailSender.SendMailAsync(new EmailDto
            {
                To = user.Email,
                Subject = "Verify your AccountFlow email",
                Body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #4F46E5;'>Welcome to AccountFlow!</h2>
                    <p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
                    <p>Please verify your email address to get started.</p>
                    <br/>
                    <a href='{verificationLink}' style='background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Verify Email Now</a>
                    <br/><br/>
                    <p style='font-size: 12px; color: #666;'>Or copy-paste this link into your browser:</p>
                    <p style='font-size: 12px; color: #666;'>{verificationLink}</p>
                    <p style='font-size: 12px; color: #999;'>This link expires in {_auth.VerificationCodeMinutes} minutes.</p>
                </div>"
            });
        }

        // Computes SHA256 hash of the input string
        private static string ComputeSha256(string input)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash);
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private string GetFrontendUrl() =>
            (_configuration.GetValue<string>("AppSettings:FrontendUrl") ?? "http://localhost:5173")
            .TrimEnd('/');

        private IFindFluent<User, User> FindByEmail(string email) =>
            _users.Find(
                user => user.Email == NormalizeEmail(email),
                new FindOptions { Collation = EmailCollation });

        // Generates a random alphanumeric string of the specified length (rejection sampling avoids modulo bias)
        private static string GenerateAlphaNumericCode(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new StringBuilder(length);

            for (var i = 0; i < length; i++)
            {
                result.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
            }

            return result.ToString();
        }

        // Counts users registered within the last 24 hours
        public async Task<long> GetDailyRegistrationCountAsync()
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            return await _users.CountDocumentsAsync(u => u.CreatedAt >= oneDayAgo);
        }

        // Counts unverified users registered within the last 24 hours
        public async Task<long> GetDailyUnverifiedCountAsync()
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            return await _users.CountDocumentsAsync(u => u.CreatedAt >= oneDayAgo && u.IsVerified == false);
        }

        public async Task<AdminStatsDto> GetDailyStatsAsync()
        {
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var stats = await _users.Aggregate()
                .Match(user => user.CreatedAt >= oneDayAgo)
                .Group(
                    _ => 1,
                    users => new AdminStatsDto
                    {
                        Registrations = users.LongCount(),
                        Unverified = users.LongCount(user => !user.IsVerified)
                    })
                .FirstOrDefaultAsync();

            return stats ?? new AdminStatsDto();
        }

        // Validates the provided refresh token and issues a new access/refresh token pair
        public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var currentHash = ComputeSha256(request.RefreshToken);
            var newRefreshToken = GenerateRefreshToken();
            var newHash = ComputeSha256(newRefreshToken);
            var newExpiry = DateTime.UtcNow.AddDays(_auth.RefreshTokenDays);

            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.HashedRefreshToken, currentHash),
                Builders<User>.Filter.Gte(u => u.RefreshTokenExpiry, DateTime.UtcNow));
            var update = Builders<User>.Update
                .Set(u => u.HashedRefreshToken, newHash)
                .Set(u => u.RefreshTokenExpiry, newExpiry);

            var user = await _users.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });

            if (user == null) return null;
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = newRefreshToken
            };
        }

        private string GenerateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(_auth.RefreshTokenBytes);
            return Convert.ToBase64String(tokenBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private async Task<string?> GenerateAndUpdateUserRefreshToken(User user, DateTime expiry)
        {
            // The plaintext token is returned to the client; only its hash is persisted.
            var refreshToken = GenerateRefreshToken();
            var hashedRefreshToken = ComputeSha256(refreshToken);
            var update = Builders<User>.Update
                .Set(u => u.HashedRefreshToken, hashedRefreshToken)
                .Set(u => u.RefreshTokenExpiry, expiry);
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                Builders<User>.Filter.Eq(u => u.HashedPassword, user.HashedPassword),
                Builders<User>.Filter.Eq(u => u.IsVerified, true));
            var result = await _users.UpdateOneAsync(filter, update);
            return result.MatchedCount == 1 ? refreshToken : null;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim> {
                new(ClaimTypes.NameIdentifier, user.Id!),
                new(ClaimTypes.Role, user.Role),
                new(SessionVersionValidator.VersionClaimType,
                    user.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
                audience: _configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_auth.AccessTokenMinutes),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<TokenResponseDto?> CreateTokenResponse(User user)
        {
            var refreshToken = await GenerateAndUpdateUserRefreshToken(
                user, DateTime.UtcNow.AddDays(_auth.RefreshTokenDays));
            if (refreshToken is null) return null;

            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = refreshToken
            };
        }
    }
}
