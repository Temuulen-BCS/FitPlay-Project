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

// Read connection string: prefer DATABASE_URL env var (Railway), fallback to appsettings
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway Postgres provides a URI like: postgresql://user:pass@host:port/dbname
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')}" +
                       $";Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No database connection string found. Set DATABASE_URL env var or ConnectionStrings:DefaultConnection in appsettings.");
}

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

builder.Services.AddDbContext<FitPlayContext>(options =>
    options.UseNpgsql(
        connectionString,
        b => b.MigrationsAssembly("FitPlay.Api")
    ));

builder.Services.AddSingleton<FitPlay.Domain.Services.IClockService, FitPlay.Domain.Services.ClockService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register gamification services
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<TrainingCompletionService>();
builder.Services.AddScoped<TrainingService>();
builder.Services.AddScoped<ClassScheduleService>();
builder.Services.AddScoped<MembershipService>();

// Register gym/sessions services
builder.Services.AddScoped<IAcademyService, AcademyService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<FitPlay.Domain.Services.IGymVisitService, FitPlay.Domain.Services.GymVisitService>();
builder.Services.AddScoped<FitPlay.Domain.Services.ClassQueueService>();
builder.Services.AddScoped<FitPlay.Domain.Services.ITrainerNotificationService, FitPlay.Domain.Services.TrainerNotificationService>();
builder.Services.AddHostedService<ClassStatusAutoCompleteService>();

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

// Auto-create database schema on startup (for fresh Railway Postgres deploy)
using (var scope = app.Services.CreateScope())
{
    var fitPlayDb = scope.ServiceProvider.GetRequiredService<FitPlayContext>();
    await fitPlayDb.Database.EnsureCreatedAsync();

    var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await identityDb.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseAuthentication();   
app.UseAuthorization();    

app.MapControllers();

// Backfill Teacher.IdentityUserId for records missing the link
using (var scope = app.Services.CreateScope())
{
    var db1 = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var db2 = scope.ServiceProvider.GetRequiredService<FitPlayContext>();

    try
    {
        var teachersWithoutIdentity = await db2.Teachers
            .Where(t => t.IdentityUserId == null || t.IdentityUserId == "")
            .ToListAsync();

        if (teachersWithoutIdentity.Any())
        {
            foreach (var teacher in teachersWithoutIdentity)
            {
                var identityUser = await db1.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email != null
                        && u.Email.ToLower() == teacher.Email.ToLower());

                if (identityUser != null)
                {
                    teacher.IdentityUserId = identityUser.Id;
                    app.Logger.LogInformation(
                        "Backfilled Teacher '{Name}' (Id={Id}) with IdentityUserId={IdentityUserId}",
                        teacher.Name, teacher.Id, identityUser.Id);
                }
            }
            await db2.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Teacher IdentityUserId backfill failed (non-fatal)");
    }
}

app.Run();



