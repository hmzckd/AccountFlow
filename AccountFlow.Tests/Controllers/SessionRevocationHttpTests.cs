using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using AccountFlow.Backend.Entities;
using AccountFlow.Backend.Models;
using AccountFlow.Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AccountFlow.Tests.Controllers;

public class SessionRevocationHttpTests
{
    [Fact]
    public async Task Reset_rejects_old_access_and_refresh_tokens_on_http_requests()
    {
        var databaseName = $"accountflow_session_test_{Guid.NewGuid():N}";
        var mongoUrl = new MongoUrlBuilder(
            Environment.GetEnvironmentVariable("ACCOUNTFLOW_TEST_MONGO_URI") ?? "mongodb://localhost:27017")
        {
            DatabaseName = databaseName
        }.ToString();
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings__DbConnection"] = mongoUrl,
            ["AppSettings__Token"] = new string('x', 32),
            ["AppSettings__Issuer"] = "AccountFlowTests",
            ["AppSettings__Audience"] = "AccountFlowTests"
        };
        var previousSettings = settings.Keys.ToDictionary(
            key => key, Environment.GetEnvironmentVariable);
        foreach (var (key, value) in settings)
            Environment.SetEnvironmentVariable(key, value);

        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
            using var client = factory.CreateClient();
            var mongo = factory.Services.GetRequiredService<MongoDbService>();

            try
            {
                const string resetToken = "http-reset-test-token";
                var user = new User
                {
                    Email = "admin@example.com",
                    IsVerified = true,
                    Role = "Admin",
                    HashedResetCode = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetToken))),
                    ResetCodeExpiry = DateTime.UtcNow.AddMinutes(5)
                };
                user.HashedPassword = new PasswordHasher<User>().HashPassword(user, "Previous1!Password");
                await mongo.Users.InsertOneAsync(user);

                var oldTokens = await LoginAsync(client, user.Email, "Previous1!Password");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldTokens.AccessToken);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/stats/daily")).StatusCode);

                var reset = await client.PostAsJsonAsync("/api/auth/password/reset", new ResetPasswordDto
                {
                    Token = resetToken,
                    NewPassword = "Replacement1!Password"
                });
                Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await client.GetAsync("/api/admin/stats/daily")).StatusCode);
                var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
                {
                    RefreshToken = oldTokens.RefreshToken
                });
                Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

                client.DefaultRequestHeaders.Authorization = null;
                var newTokens = await LoginAsync(client, user.Email, "Replacement1!Password");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/stats/daily")).StatusCode);
            }
            finally
            {
                await mongo.Database.Client.DropDatabaseAsync(databaseName);
            }
        }
        finally
        {
            foreach (var (key, value) in previousSettings)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static async Task<TokenResponseDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }
}
