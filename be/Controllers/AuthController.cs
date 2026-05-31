using System.Security.Claims;
using be.Models;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace be.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IAuthSessionStore authSessionStore) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (email is null)
        {
            return BadRequest(new { message = "A valid email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequest(new { message = "Display name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        var alreadyExists = await userManager.FindByEmailAsync(email);

        if (alreadyExists is not null)
        {
            return Conflict(new { message = "A user with that email already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(ToIdentityErrorResponse(createResult, "Unable to create user."));
        }

        var roleResult = await AddToRoleAsync(user, AppRoles.Member);

        if (!roleResult.Succeeded)
        {
            return BadRequest(ToIdentityErrorResponse(roleResult, "Unable to assign user role."));
        }

        return Ok(await CreateAuthResponseAsync(user, request.RememberMe));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (email is null || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null ||
            !user.IsActive ||
            !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(await CreateAuthResponseAsync(user, request.RememberMe));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = SessionAuthenticationDefaults.SessionIdHeaderName)] string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await authSessionStore.DeleteAsync(sessionId, HttpContext.RequestAborted);
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return await ToUserProfileAsync(user);
    }

    [Authorize(Policy = AppAuthorizationPolicies.AdminOnly)]
    [HttpGet("admin-check")]
    public ActionResult<object> AdminCheck()
    {
        return Ok(new
        {
            message = "Current user is authorized as admin.",
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
        });
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(AppUser user, bool rememberMe)
    {
        var session = await authSessionStore.CreateAsync(user, rememberMe, HttpContext.RequestAborted);

        return new AuthResponse(
            session.SessionId,
            session.ExpiresAt,
            await ToUserProfileAsync(user));
    }

    private async Task<UserProfileResponse> ToUserProfileAsync(AppUser user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            await GetPrimaryRoleAsync(user),
            user.IsActive);
    }

    private async Task<string> GetPrimaryRoleAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return AppRoles.GetPrimaryRole(roles);
    }

    private async Task<IdentityResult> AddToRoleAsync(AppUser user, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            if (!createRoleResult.Succeeded)
            {
                return createRoleResult;
            }
        }

        return await userManager.AddToRoleAsync(user, roleName);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }

    private static object ToIdentityErrorResponse(IdentityResult result, string message)
    {
        return new
        {
            message,
            errors = result.Errors.Select(error => new
            {
                error.Code,
                error.Description
            })
        };
    }
}

public sealed record RegisterRequest(string? Email, string? DisplayName, string? Password, bool RememberMe = false);

public sealed record LoginRequest(string? Email, string? Password, bool RememberMe = false);

public sealed record AuthResponse(
    string SessionId,
    DateTimeOffset ExpiresAt,
    UserProfileResponse User);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive);
