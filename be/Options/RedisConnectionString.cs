using System.Net;

namespace be.Options;

public static class RedisConnectionString
{
    public static string Resolve(IConfiguration configuration, string? configuredValue)
    {
        var rawValue = FirstNonEmpty(
            configuredValue,
            configuration.GetConnectionString("Redis"),
            Environment.GetEnvironmentVariable("REDIS_URL"));

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return Normalize(rawValue);
    }

    private static string Normalize(string rawValue)
    {
        if (!Uri.TryCreate(rawValue, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("redis" or "rediss"))
        {
            return rawValue;
        }

        var port = uri.Port > 0 ? uri.Port : 6379;
        var parts = new List<string>
        {
            $"{uri.Host}:{port}",
            "abortConnect=false"
        };

        if (uri.Scheme == "rediss")
        {
            parts.Add("ssl=true");
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var userInfoParts = uri.UserInfo.Split(':', 2);

            if (!string.IsNullOrWhiteSpace(userInfoParts[0]))
            {
                parts.Add($"user={WebUtility.UrlDecode(userInfoParts[0])}");
            }

            if (userInfoParts.Length == 2 && !string.IsNullOrWhiteSpace(userInfoParts[1]))
            {
                parts.Add($"password={WebUtility.UrlDecode(userInfoParts[1])}");
            }
        }

        if (!string.IsNullOrWhiteSpace(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            var databaseValue = uri.AbsolutePath.Trim('/');

            if (int.TryParse(databaseValue, out var database))
            {
                parts.Add($"defaultDatabase={database}");
            }
        }

        return string.Join(',', parts);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
