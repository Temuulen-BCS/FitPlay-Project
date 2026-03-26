using FitPlay_Blazor.Components;
using FitPlay_Blazor.Components.Account;
using FitPlay_Blazor.Data;
using FitPlay_Blazor.Auth;
using FitPlay.Blazor.Services;
using FitPlay.Domain.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<ApiTokenHandler>();
builder.Services.AddHttpContextAccessor();

// Register HttpClient and ApiClient for API calls
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? Environment.GetEnvironmentVariable("API_BASE_URL")
    ?? "https://localhost:7248";

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var tokenHandler = sp.GetRequiredService<ApiTokenHandler>();
    return new AuthTokenMessageHandler(httpContextAccessor, tokenHandler);
});

builder.Services.AddHttpClient<BuilderHtmlService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveMembership", policy =>
        policy.RequireClaim("membership", "active"));
});

// Database: auto-detect PostgreSQL vs SQL Server from connection string
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Railway provides DATABASE_URL as postgres://user:pass@host:port/db
// Npgsql expects key-value format: Host=...;Database=...;Username=...;Password=...
static string ConvertPostgresUrl(string url)
{
    if (!url.StartsWith("postgres://") && !url.StartsWith("postgresql://"))
        return url;

    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};"
         + $"Username={userInfo[0]};Password={userInfo[1]};"
         + "SSL Mode=Require;Trust Server Certificate=true";
}

connectionString = ConvertPostgresUrl(connectionString);

var isPostgres = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
              && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
              || connectionString.Contains("5432");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
});

// FitPlayContext for domain queries (membership checks, etc.)
builder.Services.AddDbContext<FitPlayContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connectionString);
    else
        options.UseSqlServer(connectionString);
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
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

