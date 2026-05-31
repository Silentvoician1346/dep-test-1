using System.Security.Claims;
using System.Text.Encodings.Web;
using be.Models;
using be.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace be.Security;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthSessionStore sessionStore,
    UserManager<AppUser> userManager) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SessionAuthenticationDefaults.SessionIdHeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        var sessionId = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return AuthenticateResult.NoResult();
        }

        var session = await sessionStore.GetAsync(sessionId, Context.RequestAborted);

        if (session is null)
        {
            return AuthenticateResult.Fail("Session is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(session.UserId.ToString());

        if (user is null || !user.IsActive)
        {
            await sessionStore.DeleteAsync(sessionId, Context.RequestAborted);
            return AuthenticateResult.Fail("User is inactive or no longer exists.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(
            claims,
            SessionAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SessionAuthenticationDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
