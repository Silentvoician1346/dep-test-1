using be.Data;
using be.Models;
using be.Options;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var command = args.FirstOrDefault(arg => arg is "serve" or "migrate" or "seed" or "migrate-and-seed") ?? "serve";
var builderArgs = args.Where(arg => arg != command).ToArray();
var builder = WebApplication.CreateBuilder(builderArgs);

// Add services to the container.

const string FrontendCorsPolicy = "Frontend";
var runMigrationsCommand = command is "migrate" or "migrate-and-seed";
var runSeedCommand = command is "seed" or "migrate-and-seed";
var runDatabaseCommand = runMigrationsCommand || runSeedCommand;
var port = Environment.GetEnvironmentVariable("PORT");
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtSigningKeyBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);
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

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey must be configured.");
}

if (jwtSigningKeyBytes.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 UTF-8 bytes for HS256.");
}

builder.Services.AddControllers();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();
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
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSigningKeyBytes),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Token subject is not a valid user id.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());

                if (user is null || !user.IsActive)
                {
                    context.Fail("User is inactive or no longer exists.");
                    return;
                }

                var roles = await userManager.GetRolesAsync(user);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Name, user.DisplayName)
                };

                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(
                    claims,
                    JwtBearerDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role);

                context.Principal = new ClaimsPrincipal(identity);
            }
        };
    });
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
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT bearer token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
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
