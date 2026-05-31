using System.Security.Cryptography;
using System.Text.Json;
using be.Models;
using be.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace be.Services;

public interface IAuthSessionStore
{
    Task<AuthSessionCreationResult> CreateAsync(
        AppUser user,
        bool rememberMe,
        CancellationToken cancellationToken = default);

    Task<AuthSessionRecord?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed class RedisAuthSessionStore(
    IDistributedCache cache,
    IOptions<AuthSessionOptions> options) : IAuthSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthSessionCreationResult> CreateAsync(
        AppUser user,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        var sessionId = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        var absoluteExpiresAt = now.Add(
            rememberMe ? options.Value.RememberMeAbsoluteExpiration : options.Value.AbsoluteExpiration);
        var record = new AuthSessionRecord(user.Id, now, absoluteExpiresAt, rememberMe);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiresAt,
            SlidingExpiration = options.Value.IdleTimeout
        };

        await cache.SetStringAsync(
            GetCacheKey(sessionId),
            JsonSerializer.Serialize(record, JsonOptions),
            cacheOptions,
            cancellationToken);

        return new AuthSessionCreationResult(sessionId, absoluteExpiresAt);
    }

    public async Task<AuthSessionRecord?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlausibleSessionId(sessionId))
        {
            return null;
        }

        var cacheKey = GetCacheKey(sessionId);
        var value = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var record = JsonSerializer.Deserialize<AuthSessionRecord>(value, JsonOptions);

            if (record is null || record.AbsoluteExpiresAt <= DateTimeOffset.UtcNow)
            {
                await cache.RemoveAsync(cacheKey, cancellationToken);
                return null;
            }

            return record;
        }
        catch (JsonException)
        {
            await cache.RemoveAsync(cacheKey, cancellationToken);
            return null;
        }
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return IsPlausibleSessionId(sessionId)
            ? cache.RemoveAsync(GetCacheKey(sessionId), cancellationToken)
            : Task.CompletedTask;
    }

    private string GetCacheKey(string sessionId)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sessionId));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"{options.Value.KeyPrefix}{hash}";
    }

    private static bool IsPlausibleSessionId(string sessionId)
    {
        return sessionId.Length is >= 32 and <= 256 &&
               sessionId.All(character =>
                   char.IsAsciiLetterOrDigit(character) ||
                   character is '-' or '_' or '.');
    }
}

public sealed record AuthSessionRecord(
    Guid UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset AbsoluteExpiresAt,
    bool RememberMe);

public sealed record AuthSessionCreationResult(string SessionId, DateTimeOffset ExpiresAt);
