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

var builder = WebApplication.CreateBuilder(args);


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

// Database: auto-detect PostgreSQL vs SQL Server from connection string
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("No database connection string found.");

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

connStr = ConvertPostgresUrl(connStr);

var isPostgres = connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)
              && !connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase)
              || connStr.Contains("5432");

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

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveMembership", policy =>
        policy.RequireClaim("membership", "active"));
});


var app = builder.Build();

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



