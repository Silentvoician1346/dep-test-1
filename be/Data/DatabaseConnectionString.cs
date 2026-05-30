using Npgsql;

namespace be.Data;

public static class DatabaseConnectionString
{
    public static string? Resolve(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return Normalize(configuredConnectionString);
        }

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return Normalize(databaseUrl);
        }

        return null;
    }

    private static string Normalize(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0])
        };

        var passwordSeparatorIndex = uri.UserInfo.IndexOf(':');

        if (passwordSeparatorIndex >= 0)
        {
            builder.Password = Uri.UnescapeDataString(uri.UserInfo[(passwordSeparatorIndex + 1)..]);
        }

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);

            if (parts.Length != 2)
            {
                continue;
            }

            builder[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }

        return builder.ConnectionString;
    }
}
