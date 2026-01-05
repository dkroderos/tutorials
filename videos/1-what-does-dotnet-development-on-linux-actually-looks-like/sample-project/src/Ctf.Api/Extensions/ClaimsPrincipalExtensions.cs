using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ctf.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetLoggedInUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var loggedInUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(loggedInUserId))
            throw new InvalidOperationException("User ID claim is missing.");

        if (!Guid.TryParse(loggedInUserId, out var userId))
            throw new InvalidOperationException("User ID claim is not a valid GUID.");

        return userId;
    }

    public static string? GetLoggedInUserName(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(JwtRegisteredClaimNames.Name);
    }

    public static string? GetLoggedInUserEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
