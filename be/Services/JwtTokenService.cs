using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using be.Models;
using be.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace be.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public TokenResult CreateToken(AppUser user)
    {
        var jwtOptions = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public sealed record TokenResult(string AccessToken, DateTime ExpiresAt);
