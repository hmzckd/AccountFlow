using System.Globalization;
using System.Security.Claims;
using MongoDB.Driver;

namespace AccountFlow.Backend.Services;

public sealed class SessionVersionValidator(MongoDbService mongoDbService)
{
    public const string VersionClaimType = "session_version";

    public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var versionClaim = principal.FindFirstValue(VersionClaimType);
        if (string.IsNullOrEmpty(userId) ||
            !long.TryParse(versionClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
        {
            return false;
        }

        var user = await mongoDbService.Users
            .Find(u => u.Id == userId)
            .Project(u => new { u.IsVerified, u.SessionVersion })
            .FirstOrDefaultAsync(cancellationToken);
        return user is { IsVerified: true } && user.SessionVersion == version;
    }
}
