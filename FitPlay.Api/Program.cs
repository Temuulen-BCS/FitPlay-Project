using FitPlay.Api;
using FitPlay.Api.Data;
//using FitPlay.Api.Endpoints;
using FitPlay.Api.Auth;
using Stripe;
using FitPlay.Domain.Data;
using FitPlay.Api.Services;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.AspNetCore.Identity;        
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;       
using Microsoft.OpenApi.Models;             
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Railway injects PORT env var; bind to it for production
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FitPlay.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database: build connection string from Railway PG* env vars, or fall back to appsettings
static string BuildConnectionString(IConfiguration config)
{
    var pgHost     = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort     = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser     = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

    // Railway PostgreSQL: use individual PG* variables directly
    if (pgHost != null && pgDatabase != null && pgUser != null && pgPassword != null)
        return $"Host={pgHost};Port={pgPort};Database={pgDatabase};"
             + $"Username={pgUser};Password={pgPassword};"
             + "SSL Mode=Require;Trust Server Certificate=true";

    // Local development: use appsettings.json
    return config.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No database connection string found.");
}

var connStr = BuildConnectionString(builder.Configuration);
var isPostgres = Environment.GetEnvironmentVariable("PGHOST") != null;

builder.Services.AddDbContext<FitPlayContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connStr, b => b.MigrationsAssembly("FitPlay.Api"));
    else
        options.UseSqlServer(connStr, b => b.MigrationsAssembly("FitPlay.Api").EnableRetryOnFailure());
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connStr);
    else
        options.UseSqlServer(connStr, b => b.EnableRetryOnFailure());
});

// CORS – allow the Blazor front-end origin (set via env var in production)
var allowedOrigins = builder.Configuration["AllowedOrigins"]
    ?? Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?? "https://localhost:7050";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins.Split(','))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Register gamification services
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<TrainingCompletionService>();
builder.Services.AddScoped<TrainingService>();
builder.Services.AddScoped<ClassScheduleService>();
builder.Services.AddScoped<ClassQueueService>();
builder.Services.AddScoped<MembershipService>();

// Register gym/sessions services
builder.Services.AddScoped<IAcademyService, AcademyService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

var jwtKey = (builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Missing config 'Jwt:Key'. Set env var Jwt__Key (Railway) or Jwt:Key in appsettings.json.")).Trim();
var jwtIssuer = (builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Missing config 'Jwt:Issuer'. Set env var Jwt__Issuer (Railway) or Jwt:Issuer in appsettings.json.")).Trim();
var jwtAudience = (builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Missing config 'Jwt:Audience'. Set env var Jwt__Audience (Railway) or Jwt:Audience in appsettings.json.")).Trim();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };

        // Surface the exact JWT validation error in a response header so the
        // Blazor frontend can display it (since we can't check Railway logs).
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Headers.Append("Token-Validation-Error",
                    context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                if (context.AuthenticateFailure != null)
                {
                    context.Response.Headers.Append("Token-Validation-Error",
                        context.AuthenticateFailure.Message);
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveMembership", policy =>
        policy.RequireClaim("membership", "active"));
});


var app = builder.Build();

// ── Startup diagnostics: log JWT config so we can verify Railway env vars ──
app.Logger.LogInformation(
    "JWT config → Issuer={Issuer}, Audience={Audience}, KeyLength={KeyLen}",
    jwtIssuer, jwtAudience, jwtKey.Length);

StripeConfiguration.ApiKey = builder.Configuration[$"{StripeOptions.SectionName}:SecretKey"];

// Always expose Swagger in production on Railway (useful for testing)
app.UseSwagger();
app.UseSwaggerUI();

// Exempt the Stripe webhooks from HTTPS redirection so the Stripe CLI can POST
// to http://localhost without getting a 307 redirect.
// In production (Railway), HTTPS is handled by the reverse proxy.
if (app.Environment.IsDevelopment())
{
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/api/billing/webhook")
            && !ctx.Request.Path.StartsWithSegments("/api/booking-webhook"),
        branch => branch.UseHttpsRedirection());
}

app.UseCors("AllowFrontend");

// Railway terminates TLS at its reverse proxy.  Without forwarded headers,
// ASP.NET Core sees http:// which can break JWT audience/issuer validation
// when the expected values use https://.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();   
app.UseAuthorization();    

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm",
        "Balmy", "Hot", "Sweltering", "Scorching"
    };

    return Enumerable.Range(1, 5).Select(index =>
        new
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        });
});

app.MapControllers();

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var db1 = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var db2 = scope.ServiceProvider.GetRequiredService<FitPlayContext>();

    await db1.Database.MigrateAsync();
    await db2.Database.MigrateAsync();
}

app.Run();



