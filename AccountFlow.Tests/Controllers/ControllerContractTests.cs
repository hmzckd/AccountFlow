using AccountFlow.Backend.Common;
using AccountFlow.Backend.Controllers;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountFlow.Tests.Controllers
{
    public class ControllerContractTests
    {
        [Fact]
        public async Task DailyStats_Returns_The_Combined_Service_Result()
        {
            var expected = new AdminStatsDto { Registrations = 12, Unverified = 3 };
            var controller = new AdminController(new StubAuthService(expected));

            var response = await controller.GetDailyStats();

            var ok = Assert.IsType<OkObjectResult>(response.Result);
            Assert.Same(expected, ok.Value);
        }

        [Fact]
        public async Task SendEmail_Delegates_To_The_Email_Service()
        {
            var emailService = new RecordingEmailService();
            var controller = new EmailController(emailService);
            var request = new EmailDto
            {
                To = "recipient@example.com",
                Subject = "Subject",
                Body = "Body"
            };

            var response = await controller.SendEmail(request);

            Assert.IsType<NoContentResult>(response);
            Assert.Same(request, emailService.SentEmail);
        }

        private sealed class RecordingEmailService : IEmailService
        {
            public EmailDto? SentEmail { get; private set; }

            public Task SendMailAsync(EmailDto request)
            {
                SentEmail = request;
                return Task.CompletedTask;
            }
        }

        private sealed class StubAuthService(AdminStatsDto stats) : IAuthService
        {
            public Task<AdminStatsDto> GetDailyStatsAsync() => Task.FromResult(stats);

            public Task<RegisterResult> RegisterAsync(RegisterDto request) =>
                throw new NotSupportedException();

            public Task<TokenResponseDto?> LoginAsync(LoginDto request) =>
                throw new NotSupportedException();

            public Task GeneratePasswordResetToken(string email) =>
                throw new NotSupportedException();

            public Task<User?> ResetPasswordAsync(ResetPasswordDto dto) =>
                throw new NotSupportedException();

            public Task<User?> VerifyAsync(string token) =>
                throw new NotSupportedException();

            public Task ResendVerificationAsync(string email) =>
                throw new NotSupportedException();

            public Task<long> GetDailyRegistrationCountAsync() =>
                throw new NotSupportedException();

            public Task<long> GetDailyUnverifiedCountAsync() =>
                throw new NotSupportedException();

            public Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request) =>
                throw new NotSupportedException();
        }
    }
}
