using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Endpoints;

public static class ExerciseEndpoints
{
    public static void MapExerciseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exercises").WithTags("Exercises");

        // GET: api/exercises
        group.MapGet("/", async (ApplicationDbContext db, string? trainingType, string? muscleGroup) =>
        {
            var query = db.Exercises.Where(e => e.IsActive);

            if (!string.IsNullOrEmpty(trainingType) && Enum.TryParse<TrainingType>(trainingType, out var type))
                query = query.Where(e => e.TrainingType == type);

            if (!string.IsNullOrEmpty(muscleGroup))
                query = query.Where(e => e.MuscleGroup.Contains(muscleGroup));

            var exercises = await query
                .Select(e => new ExerciseResponse(
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VideoUrl,
                    e.TrainingType.ToString(),
                    e.MuscleGroup,
                    e.IsActive
                ))
                .ToListAsync();

            return Results.Ok(exercises);
        })
        .WithName("GetExercises")
        .WithOpenApi();

        // GET: api/exercises/{id}
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var exercise = await db.Exercises
                .Where(e => e.Id == id)
                .Select(e => new ExerciseResponse(
                    e.Id,
                    e.Name,
                    e.Description,
                    e.VideoUrl,
                    e.TrainingType.ToString(),
                    e.MuscleGroup,
                    e.IsActive
                ))
                .FirstOrDefaultAsync();

            return exercise is null ? Results.NotFound() : Results.Ok(exercise);
        })
        .WithName("GetExercise")
        .WithOpenApi();

        // POST: api/exercises
        group.MapPost("/", async (ExerciseRequest request, ApplicationDbContext db) =>
        {
            var exercise = new Exercise
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                VideoUrl = request.VideoUrl ?? string.Empty,
                TrainingType = Enum.Parse<TrainingType>(request.TrainingType ?? "Functional"),
                MuscleGroup = request.MuscleGroup ?? string.Empty,
                IsActive = true
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            return Results.Created($"/api/exercises/{exercise.Id}", new ExerciseResponse(
                exercise.Id,
                exercise.Name,
                exercise.Description,
                exercise.VideoUrl,
                exercise.TrainingType.ToString(),
                exercise.MuscleGroup,
                exercise.IsActive
            ));
        })
        .WithName("CreateExercise")
        .WithOpenApi();

        // PUT: api/exercises/{id}
        group.MapPut("/{id:int}", async (int id, ExerciseRequest request, ApplicationDbContext db) =>
        {
            var exercise = await db.Exercises.FindAsync(id);
            if (exercise is null) return Results.NotFound();

            exercise.Name = request.Name;
            exercise.Description = request.Description ?? exercise.Description;
            exercise.VideoUrl = request.VideoUrl ?? exercise.VideoUrl;
            exercise.TrainingType = Enum.Parse<TrainingType>(request.TrainingType ?? exercise.TrainingType.ToString());
            exercise.MuscleGroup = request.MuscleGroup ?? exercise.MuscleGroup;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateExercise")
        .WithOpenApi();

        // DELETE: api/exercises/{id}
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var exercise = await db.Exercises.FindAsync(id);
            if (exercise is null) return Results.NotFound();

            exercise.IsActive = false; // Soft delete
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteExercise")
        .WithOpenApi();
    }
}

public record ExerciseRequest(
    string Name,
    string? Description,
    string? VideoUrl,
    string? TrainingType,
    string? MuscleGroup
);

public record ExerciseResponse(
    int Id,
    string Name,
    string Description,
    string VideoUrl,
    string TrainingType,
    string MuscleGroup,
    bool IsActive
);
