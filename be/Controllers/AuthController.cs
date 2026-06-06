using System.Security.Claims;
using be.Contracts;
using be.Extensions;
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
            return this.ApiValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] = ["A valid email is required."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return this.ApiValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.DisplayName)] = ["Display name is required."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return this.ApiValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Password)] = ["Password must be at least 8 characters."]
            });
        }

        var alreadyExists = await userManager.FindByEmailAsync(email);

        if (alreadyExists is not null)
        {
            return this.ApiProblem(
                StatusCodes.Status409Conflict,
                "A user with that email already exists.",
                ApiProblemTypes.Conflict);
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
            return this.ApiValidationProblem(
                ToIdentityErrors(createResult),
                "Unable to create user.");
        }

        var roleResult = await AddToRoleAsync(user, AppRoles.Member);

        if (!roleResult.Succeeded)
        {
            return this.ApiValidationProblem(
                ToIdentityErrors(roleResult),
                "Unable to assign user role.");
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
            return this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password.",
                ApiProblemTypes.InvalidCredentials);
        }

        var user = await userManager.FindByEmailAsync(email);

        if (user is null ||
            !user.IsActive ||
            !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password.",
                ApiProblemTypes.InvalidCredentials);
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
            return this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                ApiProblemTypes.AuthenticationRequired);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null || !user.IsActive)
        {
            return this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                ApiProblemTypes.AuthenticationRequired);
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

    private static Dictionary<string, string[]> ToIdentityErrors(IdentityResult result)
    {
        return result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
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
