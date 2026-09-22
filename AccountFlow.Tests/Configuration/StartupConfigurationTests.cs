using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace AccountFlow.Tests.Configuration;

public class StartupConfigurationTests
{
    [Fact]
    public void Startup_rejects_a_signing_key_shorter_than_32_utf8_bytes()
    {
        var exception = StartWithSetting("AppSettings:Token", "too-short");

        Assert.Contains(
            "AppSettings:Token must be at least 32 bytes when encoded as UTF-8.",
            exception.ToString());
    }

    [Theory]
    [InlineData("AppSettings:Issuer", "AppSettings:Issuer must not be empty.")]
    [InlineData("AppSettings:Audience", "AppSettings:Audience must not be empty.")]
    public void Startup_rejects_empty_required_jwt_settings(string setting, string expectedMessage)
    {
        var exception = StartWithSetting(setting, string.Empty);

        Assert.Contains(expectedMessage, exception.ToString());
    }

    [Theory]
    [InlineData("Auth:AccessTokenMinutes", "0", "AccessTokenMinutes must be between 1 and 1440.")]
    [InlineData("Auth:AccessTokenMinutes", "1441", "AccessTokenMinutes must be between 1 and 1440.")]
    [InlineData("Auth:RefreshTokenDays", "366", "RefreshTokenDays must be between 1 and 365.")]
    [InlineData("Auth:VerificationCodeMinutes", "0", "VerificationCodeMinutes must be between 1 and 1440.")]
    [InlineData("Auth:ResetCodeMinutes", "0", "ResetCodeMinutes must be between 1 and 1440.")]
    [InlineData("Auth:VerificationCodeLength", "5", "VerificationCodeLength must be between 6 and 64.")]
    [InlineData("Auth:RefreshTokenBytes", "31", "RefreshTokenBytes must be between 32 and 256.")]
    public void Startup_rejects_out_of_range_token_settings(
        string setting,
        string value,
        string expectedMessage)
    {
        var exception = StartWithSetting(setting, value);

        Assert.Contains(expectedMessage, exception.ToString());
    }

    private static Exception StartWithSetting(string setting, string value)
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("AppSettings:Token", new string('x', 32));
                builder.UseSetting(setting, value);
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });

        return Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }
}
