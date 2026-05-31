using be.Data;
using be.Models;
using be.Options;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var command = args.FirstOrDefault(arg => arg is "serve" or "migrate" or "seed" or "migrate-and-seed") ?? "serve";
var builderArgs = args.Where(arg => arg != command).ToArray();
var builder = WebApplication.CreateBuilder(builderArgs);

// Add services to the container.

const string FrontendCorsPolicy = "Frontend";
var runMigrationsCommand = command is "migrate" or "migrate-and-seed";
var runSeedCommand = command is "seed" or "migrate-and-seed";
var runDatabaseCommand = runMigrationsCommand || runSeedCommand;
var port = Environment.GetEnvironmentVariable("PORT");
var authSessionOptions = builder.Configuration.GetSection(AuthSessionOptions.SectionName).Get<AuthSessionOptions>() ??
                         new AuthSessionOptions();
var redisConnectionString = RedisConnectionString.Resolve(
    builder.Configuration,
    authSessionOptions.RedisConnectionString);
var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ??
                      Environment.GetEnvironmentVariable("FRONTEND_URL") ??
                      "http://localhost:3000,http://127.0.0.1:3000")
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .ToArray();

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

if (string.IsNullOrWhiteSpace(redisConnectionString) && !runDatabaseCommand)
{
    throw new InvalidOperationException(
        "Redis connection is not configured. Set AuthSession:RedisConnectionString, ConnectionStrings:Redis, or REDIS_URL.");
}

builder.Services.AddControllers();
builder.Services.Configure<AuthSessionOptions>(builder.Configuration.GetSection(AuthSessionOptions.SectionName));
builder.Services.PostConfigure<AuthSessionOptions>(options =>
{
    options.RedisConnectionString = redisConnectionString;
});
if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

builder.Services.AddSingleton<IAuthSessionStore, RedisAuthSessionStore>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = DatabaseConnectionString.Resolve(builder.Configuration);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection is not configured. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");
    }

    options.UseNpgsql(connectionString);
});
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IPasswordHasher<AppUser>, AppPasswordHasher>();
builder.Services
    .AddAuthentication(SessionAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationDefaults.AuthenticationScheme,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppAuthorizationPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Session", new OpenApiSecurityScheme
    {
        Name = SessionAuthenticationDefaults.SessionIdHeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Enter the opaque session id returned by /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Session", document, null),
            []
        }
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (runDatabaseCommand)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (runMigrationsCommand)
    {
        Console.WriteLine("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("EF Core migrations applied.");
    }

    if (runSeedCommand)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Console.WriteLine("Seeding demo data...");
        await DatabaseSeeder.SeedAsync(db, userManager, roleManager);
        Console.WriteLine("Demo data seeded.");
    }

    return;
}

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

var shouldApplyMigrations = app.Environment.IsDevelopment() ||
                            builder.Configuration.GetValue("Database:ApplyMigrations", false);
var shouldSeedDevelopmentData = app.Environment.IsDevelopment() ||
                                builder.Configuration.GetValue("Database:SeedDevelopmentData", false);

if (shouldApplyMigrations || shouldSeedDevelopmentData)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (shouldApplyMigrations)
    {
        await db.Database.MigrateAsync();
    }

    if (shouldSeedDevelopmentData)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await DatabaseSeeder.SeedAsync(db, userManager, roleManager);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
