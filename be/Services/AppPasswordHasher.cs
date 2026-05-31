using System.Security.Cryptography;
using be.Models;
using Microsoft.AspNetCore.Identity;

namespace be.Services;

public sealed class AppPasswordHasher : IPasswordHasher<AppUser>
{
    private readonly PasswordHasher<AppUser> identityHasher = new();

    public string HashPassword(AppUser user, string password)
    {
        return identityHasher.HashPassword(user, password);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        AppUser user,
        string hashedPassword,
        string providedPassword)
    {
        var identityResult = VerifyIdentityHash(user, hashedPassword, providedPassword);

        if (identityResult != PasswordVerificationResult.Failed)
        {
            return identityResult;
        }

        return VerifyLegacyPbkdf2Hash(providedPassword, hashedPassword)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    private PasswordVerificationResult VerifyIdentityHash(
        AppUser user,
        string hashedPassword,
        string providedPassword)
    {
        try
        {
            return identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
        catch (Exception error) when (error is FormatException or ArgumentException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static bool VerifyLegacyPbkdf2Hash(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('$');

            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
            {
                return false;
            }

            if (!int.TryParse(parts[1], out var iterations))
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expectedKey = Convert.FromBase64String(parts[3]);
            var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch (Exception error) when (error is FormatException or ArgumentException)
        {
            return false;
        }
    }
}
