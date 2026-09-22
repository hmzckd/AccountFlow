using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Options;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AccountFlow.Tests.Services;

public class AuthServiceResetPasswordTests
{
    [Fact]
    public async Task Concurrent_resets_consume_the_token_only_once()
    {
        var databaseName = $"accountflow_reset_test_{Guid.NewGuid():N}";
        var mongoUrl = new MongoUrlBuilder(
            Environment.GetEnvironmentVariable("ACCOUNTFLOW_TEST_MONGO_URI") ?? "mongodb://localhost:27017")
        {
            DatabaseName = databaseName
        }.ToString();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DbConnection"] = mongoUrl,
                ["AppSettings:Token"] = new string('x', 32),
                ["AppSettings:Issuer"] = "AccountFlowTests",
                ["AppSettings:Audience"] = "AccountFlowTests"
            })
            .Build();
        var mongo = new MongoDbService(configuration);

        try
        {
            const string resetToken = "single-use-test-token";
            var user = new User
            {
                Email = "reset@example.com",
                IsVerified = true,
                HashedResetCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetToken))),
                ResetCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                HashedRefreshToken = "existing-refresh-token-hash"
            };
            user.HashedPassword = new PasswordHasher<User>().HashPassword(user, "Previous1!Password");
            await mongo.Users.InsertOneAsync(user);
            // Existing records created before session versioning have no such field.
            await mongo.Users.UpdateOneAsync(u => u.Id == user.Id,
                Builders<User>.Update.Unset(u => u.SessionVersion));

            var auth = new AuthService(
                mongo,
                new UnusedEmailService(),
                configuration,
                Options.Create(new AuthOptions()),
                NullLogger<AuthService>.Instance);
            var sessionValidator = new SessionVersionValidator(mongo);
            var priorLogin = await auth.LoginAsync(new LoginDto
            {
                Email = user.Email,
                Password = "Previous1!Password"
            });
            Assert.NotNull(priorLogin);
            var priorPrincipal = PrincipalFrom(priorLogin.AccessToken);
            Assert.True(await sessionValidator.IsCurrentAsync(priorPrincipal));
            Assert.False(await sessionValidator.IsCurrentAsync(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id!)]))));

            var requests = Enumerable.Range(0, 8)
                .Select(i => auth.ResetPasswordAsync(new ResetPasswordDto
                {
                    Token = resetToken,
                    NewPassword = $"Password{i}!"
                }))
                .ToArray();
            var results = await Task.WhenAll(requests);

            var winner = Assert.Single(results, result => result is not null);
            var winnerIndex = Array.IndexOf(results, winner);
            var stored = await mongo.Users.Find(u => u.Id == user.Id).SingleAsync();
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword(stored, stored.HashedPassword, $"Password{winnerIndex}!"));
            Assert.Null(stored.HashedResetCode);
            Assert.Null(stored.ResetCodeExpiry);
            Assert.Null(stored.HashedRefreshToken);
            Assert.Equal(1, stored.SessionVersion);
            Assert.False(await sessionValidator.IsCurrentAsync(priorPrincipal));
            Assert.Null(await auth.RefreshTokenAsync(new RefreshTokenRequestDto
            {
                RefreshToken = priorLogin.RefreshToken
            }));
            Assert.Null(await auth.LoginAsync(new LoginDto
            {
                Email = user.Email,
                Password = "Previous1!Password"
            }));
            var newLogin = await auth.LoginAsync(new LoginDto
            {
                Email = user.Email,
                Password = $"Password{winnerIndex}!"
            });
            Assert.NotNull(newLogin);
            Assert.True(await sessionValidator.IsCurrentAsync(PrincipalFrom(newLogin.AccessToken)));
            Assert.Null(await auth.ResetPasswordAsync(new ResetPasswordDto
            {
                Token = resetToken,
                NewPassword = "AnotherPassword!"
            }));
        }
        finally
        {
            await mongo.Database.Client.DropDatabaseAsync(databaseName);
        }
    }

    private static ClaimsPrincipal PrincipalFrom(string token) => new(
        new ClaimsIdentity(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims));

    private sealed class UnusedEmailService : IEmailService
    {
        public Task SendMailAsync(EmailDto request) => throw new NotSupportedException();
    }
}
