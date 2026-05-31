namespace be.Options;

public sealed class AuthSessionOptions
{
    public const string SectionName = "AuthSession";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "dep-test-1:sessions:";

    public int IdleTimeoutMinutes { get; set; } = 120;

    public int AbsoluteExpirationDays { get; set; } = 7;

    public int RememberMeAbsoluteExpirationDays { get; set; } = 14;

    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(Math.Max(1, IdleTimeoutMinutes));

    public TimeSpan AbsoluteExpiration => TimeSpan.FromDays(Math.Max(1, AbsoluteExpirationDays));

    public TimeSpan RememberMeAbsoluteExpiration => TimeSpan.FromDays(
        Math.Max(AbsoluteExpirationDays, RememberMeAbsoluteExpirationDays));
}
