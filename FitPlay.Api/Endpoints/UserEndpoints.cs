using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // GET: api/users
        group.MapGet("/", async (ApplicationDbContext db, int? boxId, bool? activeOnly) =>
        {
            var query = db.Users.AsQueryable();

            if (boxId.HasValue)
                query = query.Where(u => u.BoxId == boxId);

            if (activeOnly ?? true)
                query = query.Where(u => u.IsActive);

            var users = await query
                .Include(u => u.Box)
                .Include(u => u.Level)
                .Select(u => new UserResponse(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.BirthDate,
                    u.Role.ToString(),
                    u.Points,
                    u.Xp,
                    u.IsActive,
                    u.BoxId,
                    u.Box != null ? u.Box.Name : null,
                    u.LevelId,
                    u.Level != null ? u.Level.Name : null,
                    u.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(users);
        })
        .WithName("GetUsers")
        .WithOpenApi();

        // GET: api/users/{id}
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.Box)
                .Include(u => u.Level)
                .Where(u => u.Id == id)
                .Select(u => new UserResponse(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.BirthDate,
                    u.Role.ToString(),
                    u.Points,
                    u.Xp,
                    u.IsActive,
                    u.BoxId,
                    u.Box != null ? u.Box.Name : null,
                    u.LevelId,
                    u.Level != null ? u.Level.Name : null,
                    u.CreatedAt
                ))
                .FirstOrDefaultAsync();

            return user is null ? Results.NotFound() : Results.Ok(user);
        })
        .WithName("GetUser")
        .WithOpenApi();

        // POST: api/users
        group.MapPost("/", async (UserRequest request, ApplicationDbContext db) =>
        {
            // Check if email already exists
            if (await db.Users.AnyAsync(u => u.Email == request.Email))
                return Results.BadRequest("Email already exists");

            // Get initial level (Beginner)
            var initialLevel = await db.Levels.OrderBy(l => l.RequiredXp).FirstOrDefaultAsync();

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone ?? string.Empty,
                BirthDate = request.BirthDate,
                Role = Enum.Parse<UserRole>(request.Role ?? "Athlete"),
                BoxId = request.BoxId,
                LevelId = initialLevel?.Id,
                Points = 0,
                Xp = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", new UserResponse(
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                user.BirthDate,
                user.Role.ToString(),
                user.Points,
                user.Xp,
                user.IsActive,
                user.BoxId,
                null,
                user.LevelId,
                initialLevel?.Name,
                user.CreatedAt
            ));
        })
        .WithName("CreateUser")
        .WithOpenApi();

        // PUT: api/users/{id}
        group.MapPut("/{id:int}", async (int id, UserRequest request, ApplicationDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            // Check if email already exists for another user
            if (await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                return Results.BadRequest("Email already exists");

            user.Name = request.Name;
            user.Email = request.Email;
            user.Phone = request.Phone ?? user.Phone;
            user.BirthDate = request.BirthDate;
            user.Role = Enum.Parse<UserRole>(request.Role ?? user.Role.ToString());
            user.BoxId = request.BoxId;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateUser")
        .WithOpenApi();

        // DELETE: api/users/{id}
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) return Results.NotFound();

            user.IsActive = false; // Soft delete
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithOpenApi();

        // POST: api/users/{id}/add-xp
        group.MapPost("/{id:int}/add-xp", async (int id, AddXpRequest request, ApplicationDbContext db) =>
        {
            var user = await db.Users.Include(u => u.Level).FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return Results.NotFound();

            user.Xp += request.Amount;
            user.Points += request.Amount;

            // Check for level up
            var newLevel = await db.Levels
                .Where(l => l.RequiredXp <= user.Xp)
                .OrderByDescending(l => l.RequiredXp)
                .FirstOrDefaultAsync();

            if (newLevel != null && newLevel.Id != user.LevelId)
            {
                user.LevelId = newLevel.Id;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { 
                user.Xp, 
                user.Points, 
                LevelId = user.LevelId, 
                LevelName = newLevel?.Name 
            });
        })
        .WithName("AddUserXp")
        .WithOpenApi();
    }
}

public record UserRequest(
    string Name,
    string Email,
    string? Phone,
    DateOnly? BirthDate,
    string? Role,
    int? BoxId
);

public record UserResponse(
    int Id,
    string Name,
    string Email,
    string Phone,
    DateOnly? BirthDate,
    string Role,
    int Points,
    int Xp,
    bool IsActive,
    int? BoxId,
    string? BoxName,
    int? LevelId,
    string? LevelName,
    DateTime CreatedAt
);

public record AddXpRequest(int Amount);
