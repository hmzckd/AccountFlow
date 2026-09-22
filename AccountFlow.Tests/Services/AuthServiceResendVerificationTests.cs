using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Options;
using AccountFlow.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AccountFlow.Tests.Services;

public class AuthServiceResendVerificationTests
{
    [Fact]
    public async Task Registration_starts_the_resend_cooldown()
    {
        await WithDatabase(async (auth, users, mail) =>
        {
            var registration = await auth.RegisterAsync(new RegisterDto
            {
                Name = "Ada",
                Surname = "Lovelace",
                Email = "new@example.com",
                Password = "Strong1!Password"
            });

            Assert.Equal(AccountFlow.Backend.Common.RegisterStatus.Success, registration.Status);
            Assert.Single(mail.Sent);
            await auth.ResendVerificationAsync("new@example.com");
            Assert.Single(mail.Sent);
            var stored = await users.Find(u => u.Email == "new@example.com").SingleAsync();
            Assert.NotNull(stored.LastVerificationEmailSentAt);
        });
    }

    [Fact]
    public async Task Expired_link_can_be_replaced_and_new_link_verifies_account()
    {
        await WithDatabase(async (auth, users, mail) =>
        {
            var oldToken = "expired-verification-code";
            var user = new User
            {
                Name = "Ada",
                Email = "ada@example.com",
                HashedVerificationCode = Hash(oldToken),
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-1),
                LastVerificationEmailSentAt = DateTime.UtcNow.AddMinutes(-20)
            };
            await users.InsertOneAsync(user);

            await auth.ResendVerificationAsync(user.Email);

            var message = Assert.Single(mail.Sent);
            var newToken = Regex.Match(message.Body, @"/verify\?token=([A-Z0-9]+)").Groups[1].Value;
            Assert.NotEmpty(newToken);
            var stored = await users.Find(u => u.Id == user.Id).SingleAsync();
            Assert.Equal(Hash(newToken), stored.HashedVerificationCode);
            Assert.True(stored.VerificationCodeExpiry > DateTime.UtcNow);

            await auth.ResendVerificationAsync(user.Email); // Cooldown must not replace the new link.
            Assert.Single(mail.Sent);
            Assert.Null(await auth.VerifyAsync(oldToken));
            Assert.NotNull(await auth.VerifyAsync(newToken));
            Assert.Null(await auth.VerifyAsync(newToken));

            await auth.ResendVerificationAsync(user.Email); // Verified accounts receive nothing.
            await auth.ResendVerificationAsync("unknown@example.com");
            Assert.Single(mail.Sent);
        });
    }

    [Fact]
    public async Task Concurrent_resend_requests_send_only_one_message()
    {
        await WithDatabase(async (auth, users, mail) =>
        {
            var user = new User
            {
                Email = "parallel@example.com",
                HashedVerificationCode = Hash("old-token"),
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-1),
                LastVerificationEmailSentAt = DateTime.UtcNow.AddMinutes(-20)
            };
            await users.InsertOneAsync(user);

            await Task.WhenAll(Enumerable.Range(0, 8)
                .Select(_ => auth.ResendVerificationAsync(user.Email)));

            Assert.Single(mail.Sent);
        });
    }

    [Fact]
    public async Task Failed_delivery_restores_previous_state_for_immediate_retry()
    {
        await WithDatabase(async (auth, users, mail) =>
        {
            var previousSentAt = DateTime.UtcNow.AddMinutes(-20);
            var user = new User
            {
                Email = "retry@example.com",
                HashedVerificationCode = Hash("old-token"),
                VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(-1),
                LastVerificationEmailSentAt = previousSentAt
            };
            await users.InsertOneAsync(user);
            var before = await users.Find(u => u.Id == user.Id).SingleAsync();

            mail.Fail = true;
            await auth.ResendVerificationAsync(user.Email);
            var afterFailure = await users.Find(u => u.Id == user.Id).SingleAsync();
            Assert.Equal(before.HashedVerificationCode, afterFailure.HashedVerificationCode);
            Assert.Equal(before.VerificationCodeExpiry, afterFailure.VerificationCodeExpiry);
            Assert.Equal(before.LastVerificationEmailSentAt, afterFailure.LastVerificationEmailSentAt);

            mail.Fail = false;
            await auth.ResendVerificationAsync(user.Email);
            Assert.Single(mail.Sent);
        });
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task WithDatabase(
        Func<AuthService, IMongoCollection<User>, RecordingEmailService, Task> test)
    {
        var databaseName = $"accountflow_verification_test_{Guid.NewGuid():N}";
        var mongoUrl = new MongoUrlBuilder(
            Environment.GetEnvironmentVariable("ACCOUNTFLOW_TEST_MONGO_URI") ?? "mongodb://localhost:27017")
        {
            DatabaseName = databaseName
        }.ToString();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DbConnection"] = mongoUrl,
                ["AppSettings:FrontendUrl"] = "http://localhost:5173"
            })
            .Build();
        var mongo = new MongoDbService(configuration);

        try
        {
            var mail = new RecordingEmailService();
            var auth = new AuthService(mongo, mail, configuration,
                Options.Create(new AuthOptions()), NullLogger<AuthService>.Instance);
            await test(auth, mongo.Users, mail);
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
