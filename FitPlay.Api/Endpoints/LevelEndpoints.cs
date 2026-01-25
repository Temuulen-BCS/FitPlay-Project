using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Endpoints;

public static class LevelEndpoints
{
    public static void MapLevelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/levels").WithTags("Levels");

        // GET: api/levels
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var levels = await db.Levels
                .OrderBy(l => l.RequiredXp)
                .Select(l => new LevelResponse(
                    l.Id,
                    l.Name,
                    l.RequiredXp,
                    l.Description,
                    l.Users.Count
                ))
                .ToListAsync();
            return Results.Ok(levels);
        })
        .WithName("GetLevels")
        .WithOpenApi();

        // GET: api/levels/{id}
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var level = await db.Levels
                .Where(l => l.Id == id)
                .Select(l => new LevelResponse(
                    l.Id,
                    l.Name,
                    l.RequiredXp,
                    l.Description,
                    l.Users.Count
                ))
                .FirstOrDefaultAsync();

            return level is null ? Results.NotFound() : Results.Ok(level);
        })
        .WithName("GetLevel")
        .WithOpenApi();

        // POST: api/levels
        group.MapPost("/", async (LevelRequest request, ApplicationDbContext db) =>
        {
            var level = new Level
            {
                Name = request.Name,
                RequiredXp = request.RequiredXp,
                Description = request.Description
            };

            db.Levels.Add(level);
            await db.SaveChangesAsync();

            return Results.Created($"/api/levels/{level.Id}", new LevelResponse(
                level.Id,
                level.Name,
                level.RequiredXp,
                level.Description,
                0
            ));
        })
        .WithName("CreateLevel")
        .WithOpenApi();

        // PUT: api/levels/{id}
        group.MapPut("/{id:int}", async (int id, LevelRequest request, ApplicationDbContext db) =>
        {
            var level = await db.Levels.FindAsync(id);
            if (level is null) return Results.NotFound();

            level.Name = request.Name;
            level.RequiredXp = request.RequiredXp;
            level.Description = request.Description;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateLevel")
        .WithOpenApi();

        // DELETE: api/levels/{id}
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var level = await db.Levels.FindAsync(id);
            if (level is null) return Results.NotFound();

            db.Levels.Remove(level);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteLevel")
        .WithOpenApi();
    }
}

public record LevelRequest(string Name, int RequiredXp, string Description);
public record LevelResponse(int Id, string Name, int RequiredXp, string Description, int UserCount);
