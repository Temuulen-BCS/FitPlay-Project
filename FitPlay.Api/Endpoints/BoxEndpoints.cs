using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Endpoints;

public static class BoxEndpoints
{
    public static void MapBoxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/boxes").WithTags("Boxes");

        // GET: api/boxes
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var boxes = await db.Boxes
                .Where(b => b.IsActive)
                .Select(b => new BoxResponse(
                    b.Id,
                    b.Name,
                    b.Address,
                    b.IsActive,
                    b.Users.Count,
                    b.Trainings.Count
                ))
                .ToListAsync();
            return Results.Ok(boxes);
        })
        .WithName("GetBoxes")
        .WithOpenApi();

        // GET: api/boxes/{id}
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var box = await db.Boxes
                .Where(b => b.Id == id)
                .Select(b => new BoxResponse(
                    b.Id,
                    b.Name,
                    b.Address,
                    b.IsActive,
                    b.Users.Count,
                    b.Trainings.Count
                ))
                .FirstOrDefaultAsync();

            return box is null ? Results.NotFound() : Results.Ok(box);
        })
        .WithName("GetBox")
        .WithOpenApi();

        // POST: api/boxes
        group.MapPost("/", async (BoxRequest request, ApplicationDbContext db) =>
        {
            var box = new Box
            {
                Name = request.Name,
                Address = request.Address,
                IsActive = true
            };

            db.Boxes.Add(box);
            await db.SaveChangesAsync();

            return Results.Created($"/api/boxes/{box.Id}", new BoxResponse(
                box.Id,
                box.Name,
                box.Address,
                box.IsActive,
                0,
                0
            ));
        })
        .WithName("CreateBox")
        .WithOpenApi();

        // PUT: api/boxes/{id}
        group.MapPut("/{id:int}", async (int id, BoxRequest request, ApplicationDbContext db) =>
        {
            var box = await db.Boxes.FindAsync(id);
            if (box is null) return Results.NotFound();

            box.Name = request.Name;
            box.Address = request.Address;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateBox")
        .WithOpenApi();

        // DELETE: api/boxes/{id}
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var box = await db.Boxes.FindAsync(id);
            if (box is null) return Results.NotFound();

            box.IsActive = false; // Soft delete
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteBox")
        .WithOpenApi();
    }
}

public record BoxRequest(string Name, string Address);
public record BoxResponse(int Id, string Name, string Address, bool IsActive, int UserCount, int TrainingCount);
