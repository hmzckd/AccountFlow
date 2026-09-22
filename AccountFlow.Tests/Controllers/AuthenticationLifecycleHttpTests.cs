using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AccountFlow.Tests.Controllers;

public class AuthenticationLifecycleHttpTests
{
    [Fact]
    public async Task User_can_register_verify_login_and_rotate_refresh_token()
    {
        await WithApplication(async (client, mongo, mail) =>
        {
            var registration = new RegisterDto
            {
                Name = "Ada",
                Surname = "Lovelace",
                Email = "New.User@Example.com",
                Password = "Strong1!Password"
            };

            var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registration);
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var verificationEmail = Assert.Single(mail.Sent);
            Assert.Equal("new.user@example.com", verificationEmail.To);
            var verificationToken = Regex.Match(
                verificationEmail.Body,
                @"/verify\?token=([A-Z0-9]+)").Groups[1].Value;
            Assert.NotEmpty(verificationToken);

            var stored = await mongo.Users.Find(user => user.Email == "new.user@example.com").SingleAsync();
            Assert.False(stored.IsVerified);
            Assert.NotEqual(registration.Password, stored.HashedPassword);
            Assert.Equal(Hash(verificationToken), stored.HashedVerificationCode);

            var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Name = registration.Name,
                Surname = registration.Surname,
                Email = registration.Email.ToUpperInvariant(),
                Password = registration.Password
            });
            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
            Assert.Single(mail.Sent);

            var loginBeforeVerification = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = registration.Email,
                Password = registration.Password
            });
            Assert.Equal(HttpStatusCode.BadRequest, loginBeforeVerification.StatusCode);

            var verifyResponse = await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyDto
            {
                Token = verificationToken
            });
            Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

            var replayResponse = await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyDto
            {
                Token = verificationToken
            });
            Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);

            var tokens = await Login(client, "NEW.USER@example.com", registration.Password);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            Assert.Equal(
                HttpStatusCode.Forbidden,
                (await client.GetAsync("/api/admin/stats/daily")).StatusCode);
            client.DefaultRequestHeaders.Authorization = null;

            var rotateResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = tokens.RefreshToken
            });
            Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
            var rotated = (await rotateResponse.Content.ReadFromJsonAsync<TokenResponseDto>())!;
            Assert.NotEqual(tokens.RefreshToken, rotated.RefreshToken);
            stored = await mongo.Users.Find(user => user.Email == "new.user@example.com").SingleAsync();
            Assert.Equal(Hash(rotated.RefreshToken), stored.HashedRefreshToken);

            var replayRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = tokens.RefreshToken
            });
            Assert.Equal(HttpStatusCode.Unauthorized, replayRefreshResponse.StatusCode);

            var currentRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
            {
                RefreshToken = rotated.RefreshToken
            });
            Assert.Equal(HttpStatusCode.OK, currentRefreshResponse.StatusCode);

            stored = await mongo.Users.Find(user => user.Email == "new.user@example.com").SingleAsync();
            Assert.True(stored.IsVerified);
            Assert.Null(stored.HashedVerificationCode);
            Assert.Null(stored.VerificationCodeExpiry);
            Assert.NotEqual(rotated.RefreshToken, stored.HashedRefreshToken);
        });
    }

    [Fact]
    public async Task Verification_email_html_encodes_registered_name()
    {
        await WithApplication(async (client, _, mail) =>
        {
            const string submittedName = "<b>Ada & Bob</b>";

            var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Name = submittedName,
                Surname = "Lovelace",
                Email = "encoded-name@example.com",
                Password = "Strong1!Password"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var verificationEmail = Assert.Single(mail.Sent);
            Assert.DoesNotContain(submittedName, verificationEmail.Body);
            Assert.Contains("&lt;b&gt;Ada &amp; Bob&lt;/b&gt;", verificationEmail.Body);
        });
    }

    [Fact]
    public async Task Password_reset_request_does_not_reveal_account_existence()
    {
        await WithApplication(async (client, mongo, mail) =>
        {
            var user = new User
            {
                Email = "known@example.com",
                IsVerified = true
            };
            user.HashedPassword = new PasswordHasher<User>()
                .HashPassword(user, "Strong1!Password");
            await mongo.Users.InsertOneAsync(user);

            var knownResponse = await client.PostAsJsonAsync(
                "/api/auth/password/reset-request",
                new ResetPasswordRequestDto { Email = user.Email });
            var unknownResponse = await client.PostAsJsonAsync(
                "/api/auth/password/reset-request",
                new ResetPasswordRequestDto { Email = "unknown@example.com" });

            Assert.Equal(HttpStatusCode.OK, knownResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
            Assert.Equal(
                await knownResponse.Content.ReadAsStringAsync(),
                await unknownResponse.Content.ReadAsStringAsync());

            var resetEmail = Assert.Single(mail.Sent);
            var resetToken = Regex.Match(
                resetEmail.Body,
                @"/reset\?token=([A-Za-z0-9_-]+)").Groups[1].Value;
            Assert.NotEmpty(resetToken);

            var stored = await mongo.Users.Find(candidate => candidate.Id == user.Id).SingleAsync();
            Assert.Equal(Hash(resetToken), stored.HashedResetCode);
            Assert.NotEqual(resetToken, stored.HashedResetCode);
            Assert.True(stored.ResetCodeExpiry > DateTime.UtcNow);
        });
    }

    [Fact]
    public async Task Registration_is_rolled_back_when_verification_email_fails()
    {
        await WithApplication(async (client, mongo, mail) =>
        {
            mail.Fail = true;

            var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                Name = "Grace",
                Surname = "Hopper",
                Email = "grace@example.com",
                Password = "Strong1!Password"
            });

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Empty(mail.Sent);
            Assert.Equal(0, await mongo.Users.CountDocumentsAsync(_ => true));
        });
    }

    private static async Task<TokenResponseDto> Login(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static async Task WithApplication(
        Func<HttpClient, MongoDbService, RecordingEmailService, Task> test)
    {
        var databaseName = $"accountflow_lifecycle_test_{Guid.NewGuid():N}";
        var mongoUrl = new MongoUrlBuilder(
            Environment.GetEnvironmentVariable("ACCOUNTFLOW_TEST_MONGO_URI") ?? "mongodb://localhost:27017")
        {
            DatabaseName = databaseName
        }.ToString();
        var mail = new RecordingEmailService();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("AppSettings:Token", new string('x', 32));
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DbConnection"] = mongoUrl,
                        ["AppSettings:Token"] = new string('x', 32),
                        ["AppSettings:Issuer"] = "AccountFlowTests",
                        ["AppSettings:Audience"] = "AccountFlowTests",
                        ["AppSettings:FrontendUrl"] = "http://localhost:5173",
                        ["Auth:SendVerificationEmails"] = "true"
                    }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailService>();
                    services.AddSingleton<IEmailService>(mail);
                });
            });
        using var client = factory.CreateClient();
        var mongo = factory.Services.GetRequiredService<MongoDbService>();

        try
        {
            await test(client, mongo, mail);
        }
        finally
        {
            await mongo.Database.Client.DropDatabaseAsync(databaseName);
        }
    }

    private sealed class RecordingEmailService : IEmailService
    {
        public ConcurrentQueue<EmailDto> Sent { get; } = new();
        public bool Fail { get; set; }

        public Task SendMailAsync(EmailDto request)
        {
            if (Fail) throw new InvalidOperationException("SMTP unavailable");
            Sent.Enqueue(request);
            return Task.CompletedTask;
        }
    }
}
