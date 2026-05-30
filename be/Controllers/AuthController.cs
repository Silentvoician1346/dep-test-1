using System.Security.Claims;
using be.Data;
using be.Models;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtTokenService jwtTokenService) : ControllerBase
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

        var alreadyExists = await db.AppUsers.AnyAsync(user => user.Email == email);

        if (alreadyExists)
        {
            return Conflict(new { message = "A user with that email already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = "member",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();

        return Ok(CreateAuthResponse(user));
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

        var user = await db.AppUsers.SingleOrDefaultAsync(candidate => candidate.Email == email);

        if (user is null ||
            !user.IsActive ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(CreateAuthResponse(user));
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

        var user = await db.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId);

        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return ToUserProfile(user);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("admin-check")]
    public ActionResult<object> AdminCheck()
    {
        return Ok(new
        {
            message = "Current user is authorized as admin.",
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var token = jwtTokenService.CreateToken(user);

        return new AuthResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAt,
            ToUserProfile(user));
    }

    private static UserProfileResponse ToUserProfile(AppUser user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.IsActive);
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
}

public sealed record RegisterRequest(string? Email, string? DisplayName, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    UserProfileResponse User);

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive);
