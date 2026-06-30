using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CarMarketplace.API.Configuration;
using CarMarketplace.API.Filters;
using CarMarketplace.API.Hubs;
using CarMarketplace.API.Interfaces;
using CarMarketplace.API.Middleware;
using CarMarketplace.API.Services;
using CarMarketplace.API.Swagger;
using CarMarketplace.Application.Configuration;
using CarMarketplace.Application.Interfaces;
using CarMarketplace.Application.Services;
using CarMarketplace.Application.Validators;
using CarMarketplace.Infrastructure.Data;
using CarMarketplace.Infrastructure.Repositories;
using CarMarketplace.Infrastructure.Services;
using CarMarketplace.Infrastructure.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Bind and validate JWT settings (Application layer)
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.SecretKey),
        $"{JwtSettings.SectionName}:SecretKey is required.")
    .Validate(settings => settings.SecretKey.Length >= JwtSettingsStartupValidator.MinimumSecretLength,
        $"{JwtSettings.SectionName}:SecretKey must be at least {JwtSettingsStartupValidator.MinimumSecretLength} characters.")
    .Validate(
        settings => builder.Environment.IsDevelopment()
                    || !string.Equals(
                        settings.SecretKey,
                        JwtSettingsStartupValidator.DefaultSecretPlaceholder,
                        StringComparison.Ordinal),
        $"{JwtSettings.SectionName}:SecretKey cannot use default placeholder outside Development.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer),
        $"{JwtSettings.SectionName}:Issuer is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience),
        $"{JwtSettings.SectionName}:Audience is required.")
    .Validate(settings => settings.ExpirationInMinutes > 0,
        $"{JwtSettings.SectionName}:ExpirationInMinutes must be greater than 0.")
    .Validate(settings => settings.RefreshTokenExpirationDays > 0,
        $"{JwtSettings.SectionName}:RefreshTokenExpirationDays must be greater than 0.")
    .ValidateOnStart();

var jwtStartupSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
JwtSettingsStartupValidator.ValidateOrThrow(jwtStartupSettings, builder.Environment.IsDevelopment());

builder.Services
    .AddOptions<CorsSettings>()
    .Bind(builder.Configuration.GetSection(CorsSettings.SectionName))
    .Validate(
        settings => settings.AllowedOrigins.Any(origin => !string.IsNullOrWhiteSpace(origin)),
        $"{CorsSettings.SectionName}:AllowedOrigins must contain at least one value.")
    .ValidateOnStart();

builder.Services
    .AddOptions<RefreshTokenCookieSettings>()
    .Bind(builder.Configuration.GetSection(RefreshTokenCookieSettings.SectionName))
    .Validate(
        settings => string.Equals(settings.Path, "/api/auth", StringComparison.Ordinal),
        $"{RefreshTokenCookieSettings.SectionName}:Path must be '/api/auth'.")
    .Validate(settings => settings.SameSite is SameSiteMode.Strict or SameSiteMode.Lax or SameSiteMode.None,
        $"{RefreshTokenCookieSettings.SectionName}:SameSite must be Strict, Lax, or None.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ReverseProxySettings>()
    .Bind(builder.Configuration.GetSection(ReverseProxySettings.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<PasswordResetEmailSettings>()
    .Bind(builder.Configuration.GetSection(PasswordResetEmailSettings.SectionName));

var passwordResetEmailStartupSettings =
    builder.Configuration.GetSection(PasswordResetEmailSettings.SectionName).Get<PasswordResetEmailSettings>()
    ?? new PasswordResetEmailSettings();
using (var startupLoggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole()))
{
    var startupLogger = startupLoggerFactory.CreateLogger("Startup");
    PasswordResetEmailStartupValidator.ValidateOrWarn(
        passwordResetEmailStartupSettings,
        builder.Environment,
        startupLogger);
}

// FluentValidation: validators live in Application layer
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDTOValidator>();

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FluentValidationFilter>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<IUserConnectionTracker, InMemoryUserConnectionTracker>();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CarMarketplace.API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter JWT Bearer token only"
    };

    c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(
                referenceId: "Bearer",
                hostDocument: document,
                externalResource: null!),
            new List<string>()
        }
    });

    c.OperationFilter<AuthorizeOperationFilter>();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure DbContext (Infrastructure)
const string DevelopmentDefaultConnectionString =
    "Server=(localdb)\\mssqllocaldb;Database=CarMarketplaceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    using var startupLoggerFactory = LoggerFactory.Create(logging => logging.AddSimpleConsole());
    var startupLogger = startupLoggerFactory.CreateLogger("Startup");

    if (builder.Environment.IsDevelopment())
    {
        startupLogger.LogWarning(
            "Connection string 'DefaultConnection' is missing. Falling back to the LocalDB default. " +
            "Set ConnectionStrings:DefaultConnection in appsettings.Development.json or use ConnectionStrings__DefaultConnection.");
        connectionString = DevelopmentDefaultConnectionString;
    }
    else
    {
        startupLogger.LogCritical(
            "Connection string 'DefaultConnection' is missing. Configure ConnectionStrings:DefaultConnection in appsettings.{EnvironmentName}.json " +
            "or set ConnectionStrings__DefaultConnection.",
            builder.Environment.EnvironmentName);
        throw new InvalidOperationException("Missing required connection string 'DefaultConnection'.");
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register repositories and Unit of Work (same scoped AppDbContext shared by all)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Refresh token: settings from JwtSettings, secure generator in Infrastructure
builder.Services.AddSingleton<IRefreshTokenSettings>(sp => sp.GetRequiredService<IOptions<JwtSettings>>().Value);
builder.Services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

// Register Application services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMessageRealtimeNotifier, SignalRMessageRealtimeNotifier>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPasswordResetEmailService, SmtpPasswordResetEmailService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICarImageUploadService, CarImageUploadService>();
builder.Services.AddScoped<IProfileAvatarUploadService, ProfileAvatarUploadService>();
builder.Services.AddScoped<IRefreshTokenCookieService, RefreshTokenCookieService>();
builder.Services.AddScoped<IRefreshRequestCsrfProtectionService, RefreshRequestCsrfProtectionService>();

// Configure JWT Authentication (Microsoft.AspNetCore.Authentication.JwtBearer, symmetric key)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1) // Allow 1 min clock variance
        };

        // Allow SignalR clients to send the JWT access token via query string during WebSocket negotiation.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken)
                    && path.StartsWithSegments("/hubs/chat", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenSecurityStamp = context.Principal?.FindFirstValue("security_stamp");

                if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    context.Fail("Invalid token security stamp.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var currentSecurityStamp = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.SecurityStamp)
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (!string.Equals(currentSecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                {
                    context.Fail("Invalid token security stamp.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
var reverseProxySettings = builder.Configuration
    .GetSection(ReverseProxySettings.SectionName)
    .Get<ReverseProxySettings>() ?? new ReverseProxySettings();

var knownProxies = reverseProxySettings.KnownProxies ?? [];
var knownNetworks = reverseProxySettings.KnownNetworks ?? [];

if (!builder.Environment.IsDevelopment()
    && (knownProxies.Length == 0 && knownNetworks.Length == 0))
{
    throw new InvalidOperationException(
        $"{ReverseProxySettings.SectionName} must configure at least one known proxy or network in non-development environments.");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var knownProxy in knownProxies)
    {
        if (!IPAddress.TryParse(knownProxy, out var proxyAddress))
        {
            throw new InvalidOperationException(
                $"{ReverseProxySettings.SectionName}:KnownProxies contains invalid IP address '{knownProxy}'.");
        }

        options.KnownProxies.Add(proxyAddress);
    }

    foreach (var knownNetwork in knownNetworks)
    {
        if (!TryParseCidr(knownNetwork, out var network))
        {
            throw new InvalidOperationException(
                $"{ReverseProxySettings.SectionName}:KnownNetworks contains invalid CIDR '{knownNetwork}'.");
        }

        options.KnownNetworks.Add(network!);
    }
});

// Configure global IP-based rate limiting (100 requests/minute per IP)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("SensitiveAuth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too Many Requests", token);
    };
});

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>()?.AllowedOrigins
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException($"{CorsSettings.SectionName}:AllowedOrigins must contain at least one origin.");
}

ValidateAllowedOriginsOrThrow(allowedOrigins);

var refreshCookieStartupSettings = builder.Configuration
    .GetSection(RefreshTokenCookieSettings.SectionName)
    .Get<RefreshTokenCookieSettings>() ?? new RefreshTokenCookieSettings();

if (refreshCookieStartupSettings.SameSite == SameSiteMode.None)
{
    var nonHttpsOrigins = allowedOrigins
        .Where(origin => !origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (!builder.Environment.IsDevelopment() && nonHttpsOrigins.Length > 0)
    {
        throw new InvalidOperationException(
            $"{CorsSettings.SectionName}:AllowedOrigins must use HTTPS when {RefreshTokenCookieSettings.SectionName}:SameSite is None.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Marketplace API v1");
        c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
}

app.Use(async (context, next) =>
{
    var isSwaggerPath = context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
    if (!isSwaggerPath)
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["X-XSS-Protection"] = "1; mode=block";
    }

    await next();
});

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/health/security", (
            HttpContext context,
            IOptions<CorsSettings> corsOptions,
            IOptions<RefreshTokenCookieSettings> refreshCookieOptions) =>
        {
            var corsOriginsConfigured = corsOptions.Value.AllowedOrigins.Any(origin => !string.IsNullOrWhiteSpace(origin));
            return Results.Ok(new
            {
                httpsDetected = context.Request.IsHttps,
                forwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString(),
                corsOriginsConfigured,
                refreshCookieSecureRequired = refreshCookieOptions.Value.SameSite == SameSiteMode.None || !app.Environment.IsDevelopment()
            });
        })
        .AllowAnonymous()
        .ExcludeFromDescription();
}

app.MapControllers();

// Apply migrations on startup, then seed default Admin user if none exists.
using (var scope = app.Services.CreateScope())
{
    const int MinSeedAdminPasswordLength = 12;
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        db.Database.Migrate();
        startupLogger.LogInformation("Database migrations are applied.");

        var startupLastSeen = DateTime.UtcNow;
        var resetOnlineUsers = await db.Users
            .Where(user => user.IsOnline)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.IsOnline, false)
                .SetProperty(user => user.LastSeen, (DateTime?)startupLastSeen));
        if (resetOnlineUsers > 0)
        {
            startupLogger.LogInformation(
                "Reset {UserCount} stale online presence records during startup.",
                resetOnlineUsers);
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(
            ex,
            "Database initialization failed. Verify ConnectionStrings:DefaultConnection and ensure SQL Server or LocalDB is installed.");
        throw;
    }

    var adminPassword = app.Configuration["SeedAdmin:Password"];
    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        startupLogger.LogWarning(
            "Seed admin password is not configured (SeedAdmin:Password / SeedAdmin__Password). Admin seeding is skipped.");
    }
    else if (adminPassword.Length < MinSeedAdminPasswordLength)
    {
        startupLogger.LogWarning(
            "Seed admin password is too short. Minimum required length is {MinLength}. Admin seeding is skipped.",
            MinSeedAdminPasswordLength);
    }
    else
    {
        await DataSeeder.EnsureAdminUserAsync(db, adminPassword);
    }
}

app.Run();

static bool TryParseCidr(string cidr, out Microsoft.AspNetCore.HttpOverrides.IPNetwork? network)
{
    network = default;
    if (string.IsNullOrWhiteSpace(cidr))
    {
        return false;
    }

    var segments = cidr.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (segments.Length != 2)
    {
        return false;
    }

    if (!IPAddress.TryParse(segments[0], out var prefixAddress))
    {
        return false;
    }

    if (!int.TryParse(segments[1], out var prefixLength))
    {
        return false;
    }

    var maxPrefixLength = prefixAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
    if (prefixLength < 0 || prefixLength > maxPrefixLength)
    {
        return false;
    }

    network = new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefixAddress, prefixLength);
    return true;
}

static void ValidateAllowedOriginsOrThrow(IEnumerable<string> origins)
{
    foreach (var origin in origins)
    {
        if (origin.Contains('*'))
        {
            throw new InvalidOperationException($"{CorsSettings.SectionName}:AllowedOrigins cannot contain wildcard entries.");
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"{CorsSettings.SectionName}:AllowedOrigins contains invalid URI '{origin}'.");
        }

        if (!string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{CorsSettings.SectionName}:AllowedOrigins entry '{origin}' must be scheme + host (+ optional port) without path/query.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{CorsSettings.SectionName}:AllowedOrigins entry '{origin}' must use http or https.");
        }
    }
}
