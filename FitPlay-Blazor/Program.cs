using FitPlay_Blazor.Components;
using FitPlay_Blazor.Components.Account;
using FitPlay_Blazor.Data;
using FitPlay_Blazor.Auth;
using FitPlay.Blazor.Services;
using FitPlay.Domain.Data;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static string? FindEnvPath(string startDirectory)
{
    var dir = new DirectoryInfo(startDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (System.IO.File.Exists(candidate))
        {
            return candidate;
        }

        dir = dir.Parent;
    }

    return null;
}

static void LoadEnvFileIntoProcess(string path)
{
    foreach (var rawLine in System.IO.File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#"))
        {
            continue;
        }

        var sep = line.IndexOf('=');
        if (sep <= 0)
        {
            continue;
        }

        var key = line[..sep].Trim();
        var value = line[(sep + 1)..].Trim();

        if ((value.StartsWith("\"") && value.EndsWith("\""))
            || (value.StartsWith("'") && value.EndsWith("'")))
        {
            value = value[1..^1];
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var envPath = FindEnvPath(Directory.GetCurrentDirectory());
if (envPath is not null)
{
    LoadEnvFileIntoProcess(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Railway injects PORT env var; bind to it for production.
// In local development, rely on launchSettings/Kestrel defaults.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();

// Persist Data Protection keys so auth cookies survive container redeployments.
// Railway ephemeral filesystems lose keys on each deploy; /app/keys is a stable
// path within the container lifetime. For cross-deploy persistence, mount a
// Railway volume at /app/keys.
var keysDir = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH") ?? "/app/keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("FitPlay-Blazor");

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ApiTokenHandler>();
builder.Services.AddSingleton<IClockService, ClockService>();
builder.Services.AddHttpContextAccessor();

// Register HttpClient and ApiClient for API calls
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? Environment.GetEnvironmentVariable("API_BASE_URL")
    ?? "http://localhost:5179";

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<BuilderHtmlService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// ── External authentication providers ──
// Credentials are read from Configuration (appsettings or env vars).
// Use env vars on Railway: Authentication__Google__ClientId, etc.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
var fbAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrEmpty(fbAppId) && !string.IsNullOrEmpty(fbAppSecret))
{
    builder.Services.AddAuthentication().AddFacebook(options =>
    {
        options.AppId = fbAppId;
        options.AppSecret = fbAppSecret;
    });
}

var ghClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var ghClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrEmpty(ghClientId) && !string.IsNullOrEmpty(ghClientSecret))
{
    builder.Services.AddAuthentication().AddGitHub(options =>
    {
        options.ClientId = ghClientId;
        options.ClientSecret = ghClientSecret;
        options.Scope.Add("user:email");
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveMembership", policy =>
        policy.RequireClaim("membership", "active"));
});

// Database: build connection string from Railway PG* env vars, or fall back to appsettings
static string BuildConnectionString(IConfiguration config)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    var pgHost     = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort     = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser     = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')}"
             + $";Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }

    // Railway PostgreSQL: use individual PG* variables directly
    if (pgHost != null && pgDatabase != null && pgUser != null && pgPassword != null)
        return $"Host={pgHost};Port={pgPort};Database={pgDatabase};"
             + $"Username={pgUser};Password={pgPassword};"
             + "SSL Mode=Require;Trust Server Certificate=true";

    // Local development: use appsettings.json
    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

var connectionString = BuildConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// FitPlayContext for domain queries (membership checks, etc.)
builder.Services.AddDbContext<FitPlayContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Inject membership claim into the Blazor Identity principal at request time
builder.Services.AddScoped<IClaimsTransformation, MembershipClaimsTransformation>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// ── Startup diagnostics: log JWT config so we can verify Railway env vars ──
{
    // Check each source independently to find exactly where the key comes from
    var envKey = Environment.GetEnvironmentVariable("Jwt__Key");
    var cfgKey = app.Configuration["Jwt:Key"];
    var finalKey = (envKey ?? cfgKey ?? "").Trim();

    app.Logger.LogWarning(
        "JWT-DIAG: EnvVar Jwt__Key is {EnvStatus} (len={EnvLen}), IConfig Jwt:Key len={CfgLen}, FinalKey len={FinalLen}",
        envKey is null ? "NULL" : "SET",
        envKey?.Length ?? 0,
        cfgKey?.Length ?? 0,
        finalKey.Length);

    // List ALL env vars containing "jwt" (case-insensitive) to find misnamed vars
    var jwtEnvVars = Environment.GetEnvironmentVariables()
        .Cast<System.Collections.DictionaryEntry>()
        .Where(e => e.Key.ToString()!.Contains("jwt", StringComparison.OrdinalIgnoreCase)
                  || e.Key.ToString()!.Contains("Jwt", StringComparison.OrdinalIgnoreCase))
        .Select(e => $"{e.Key}=(len:{e.Value?.ToString()?.Length ?? 0})")
        .ToList();
    app.Logger.LogWarning(
        "JWT-DIAG: Found {Count} JWT-related env vars: [{Vars}]",
        jwtEnvVars.Count,
        jwtEnvVars.Count > 0 ? string.Join(", ", jwtEnvVars) : "NONE");

    var jwtKeyHash = finalKey.Length > 0
        ? Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(finalKey)))[..16]
        : "(no key)";

    var jwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer")
        ?? app.Configuration["Jwt:Issuer"] ?? "(not set)";
    var jwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience")
        ?? app.Configuration["Jwt:Audience"] ?? "(not set)";

    app.Logger.LogWarning(
        "JWT config → Issuer={Issuer}, Audience={Audience}, KeyLength={KeyLen}, KeyHash={KeyHash}",
        jwtIssuer, jwtAudience, finalKey.Length, jwtKeyHash);
    app.Logger.LogInformation(
        "ApiBaseUrl → {ApiBaseUrl}",
        app.Configuration["ApiBaseUrl"]
            ?? Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? "(not set)");
}

// Seed Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
foreach (var role in new[] { "Trainer", "User", "GymAdmin" })
{
    var existingRole = await roleManager.FindByNameAsync(role);
    if (existingRole is null)
    {
        await roleManager.CreateAsync(new IdentityRole(role));
    }
}
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    // HTTPS redirection disabled for development - using HTTP only
    // app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // Railway terminates TLS at the reverse proxy; skip HTTPS redirect & HSTS in production
}
app.UseStaticFiles();

// Railway (and most cloud hosts) terminate TLS at a reverse proxy and forward
// HTTP to the container.  Without this, ASP.NET Core sees every request as
// http:// which breaks cookie Secure flags and scheme-dependent auth logic.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

