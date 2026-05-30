using System.Security.Claims;
using be.Security;

namespace be.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id claim is missing or invalid.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole(AppRoles.Admin);
    }
}
