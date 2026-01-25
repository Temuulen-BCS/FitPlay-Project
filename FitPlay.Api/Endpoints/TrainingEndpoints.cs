using Microsoft.EntityFrameworkCore;
using FitPlay.Api.Data;
using FitPlay.Domain.Model;

namespace FitPlay.Api.Endpoints;

public static class TrainingEndpoints
{
    public static void MapTrainingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/trainings").WithTags("Trainings");

        // GET: api/trainings
        group.MapGet("/", async (ApplicationDbContext db, int? boxId, int? trainerId, DateTime? fromDate) =>
        {
            var query = db.Trainings.Where(t => t.IsActive);

            if (boxId.HasValue)
                query = query.Where(t => t.BoxId == boxId);

            if (trainerId.HasValue)
                query = query.Where(t => t.TrainerId == trainerId);

            if (fromDate.HasValue)
                query = query.Where(t => t.Date >= fromDate.Value);

            var trainings = await query
                .Include(t => t.Box)
                .Include(t => t.Trainer)
                .Include(t => t.Athletes)
                .OrderByDescending(t => t.Date)
                .Select(t => new TrainingResponse(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.Type.ToString(),
                    t.Date,
                    t.Duration,
                    t.Points,
                    t.MaxAthletes,
                    t.IsActive,
                    t.BoxId,
                    t.Box != null ? t.Box.Name : null,
                    t.TrainerId,
                    t.Trainer != null ? t.Trainer.Name : null,
                    t.Athletes.Count,
                    t.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(trainings);
        })
        .WithName("GetTrainings")
        .WithOpenApi();

        // GET: api/trainings/{id}
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var training = await db.Trainings
                .Include(t => t.Box)
                .Include(t => t.Trainer)
                .Include(t => t.Athletes)
                .Include(t => t.TrainingExercises)
                    .ThenInclude(te => te.Exercise)
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            if (training is null) return Results.NotFound();

            var response = new TrainingDetailResponse(
                training.Id,
                training.Name,
                training.Description,
                training.Type.ToString(),
                training.Date,
                training.Duration,
                training.Points,
                training.MaxAthletes,
                training.IsActive,
                training.BoxId,
                training.Box?.Name,
                training.TrainerId,
                training.Trainer?.Name,
                training.Athletes.Select(a => new AthleteInfo(a.Id, a.Name, a.Email)).ToList(),
                training.TrainingExercises.OrderBy(te => te.Order).Select(te => new TrainingExerciseInfo(
                    te.Id,
                    te.ExerciseId,
                    te.Exercise.Name,
                    te.Group,
                    te.Sets,
                    te.Repetitions,
                    te.Weight,
                    te.RestTime,
                    te.Notes,
                    te.Order
                )).ToList(),
                training.CreatedAt
            );

            return Results.Ok(response);
        })
        .WithName("GetTraining")
        .WithOpenApi();

        // POST: api/trainings
        group.MapPost("/", async (TrainingRequest request, ApplicationDbContext db) =>
        {
            var training = new Training
            {
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Type = Enum.Parse<TrainingType>(request.Type ?? "Functional"),
                Date = request.Date,
                Duration = TimeSpan.FromMinutes(request.DurationMinutes),
                Points = request.Points,
                MaxAthletes = request.MaxAthletes,
                BoxId = request.BoxId,
                TrainerId = request.TrainerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Trainings.Add(training);
            await db.SaveChangesAsync();

            return Results.Created($"/api/trainings/{training.Id}", new TrainingResponse(
                training.Id,
                training.Name,
                training.Description,
                training.Type.ToString(),
                training.Date,
                training.Duration,
                training.Points,
                training.MaxAthletes,
                training.IsActive,
                training.BoxId,
                null,
                training.TrainerId,
                null,
                0,
                training.CreatedAt
            ));
        })
        .WithName("CreateTraining")
        .WithOpenApi();

        // PUT: api/trainings/{id}
        group.MapPut("/{id:int}", async (int id, TrainingRequest request, ApplicationDbContext db) =>
        {
            var training = await db.Trainings.FindAsync(id);
            if (training is null) return Results.NotFound();

            training.Name = request.Name;
            training.Description = request.Description ?? training.Description;
            training.Type = Enum.Parse<TrainingType>(request.Type ?? training.Type.ToString());
            training.Date = request.Date;
            training.Duration = TimeSpan.FromMinutes(request.DurationMinutes);
            training.Points = request.Points;
            training.MaxAthletes = request.MaxAthletes;
            training.BoxId = request.BoxId;
            training.TrainerId = request.TrainerId;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("UpdateTraining")
        .WithOpenApi();

        // DELETE: api/trainings/{id}
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var training = await db.Trainings.FindAsync(id);
            if (training is null) return Results.NotFound();

            training.IsActive = false; // Soft delete
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteTraining")
        .WithOpenApi();

        // POST: api/trainings/{id}/exercises
        group.MapPost("/{id:int}/exercises", async (int id, TrainingExerciseRequest request, ApplicationDbContext db) =>
        {
            var training = await db.Trainings.FindAsync(id);
            if (training is null) return Results.NotFound();

            var exercise = await db.Exercises.FindAsync(request.ExerciseId);
            if (exercise is null) return Results.BadRequest("Exercise not found");

            var maxOrder = await db.TrainingExercises
                .Where(te => te.TrainingId == id)
                .MaxAsync(te => (int?)te.Order) ?? 0;

            var trainingExercise = new TrainingExercise
            {
                TrainingId = id,
                ExerciseId = request.ExerciseId,
                Group = request.Group ?? "WOD",
                Sets = request.Sets,
                Repetitions = request.Repetitions ?? string.Empty,
                Weight = request.Weight ?? string.Empty,
                RestTime = request.RestTime ?? string.Empty,
                Notes = request.Notes ?? string.Empty,
                Order = request.Order ?? maxOrder + 1
            };

            db.TrainingExercises.Add(trainingExercise);
            await db.SaveChangesAsync();

            return Results.Created($"/api/trainings/{id}/exercises/{trainingExercise.Id}", new TrainingExerciseInfo(
                trainingExercise.Id,
                trainingExercise.ExerciseId,
                exercise.Name,
                trainingExercise.Group,
                trainingExercise.Sets,
                trainingExercise.Repetitions,
                trainingExercise.Weight,
                trainingExercise.RestTime,
                trainingExercise.Notes,
                trainingExercise.Order
            ));
        })
        .WithName("AddExerciseToTraining")
        .WithOpenApi();

        // DELETE: api/trainings/{id}/exercises/{exerciseId}
        group.MapDelete("/{id:int}/exercises/{trainingExerciseId:int}", async (int id, int trainingExerciseId, ApplicationDbContext db) =>
        {
            var trainingExercise = await db.TrainingExercises
                .FirstOrDefaultAsync(te => te.TrainingId == id && te.Id == trainingExerciseId);

            if (trainingExercise is null) return Results.NotFound();

            db.TrainingExercises.Remove(trainingExercise);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("RemoveExerciseFromTraining")
        .WithOpenApi();

        // POST: api/trainings/{id}/athletes/{userId}
        group.MapPost("/{id:int}/athletes/{userId:int}", async (int id, int userId, ApplicationDbContext db) =>
        {
            var training = await db.Trainings
                .Include(t => t.Athletes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training is null) return Results.NotFound("Training not found");

            var user = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound("User not found");

            if (training.Athletes.Count >= training.MaxAthletes)
                return Results.BadRequest("Training is full");

            if (training.Athletes.Any(a => a.Id == userId))
                return Results.BadRequest("User already registered for this training");

            training.Athletes.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "User registered for training successfully" });
        })
        .WithName("RegisterAthleteForTraining")
        .WithOpenApi();

        // DELETE: api/trainings/{id}/athletes/{userId}
        group.MapDelete("/{id:int}/athletes/{userId:int}", async (int id, int userId, ApplicationDbContext db) =>
        {
            var training = await db.Trainings
                .Include(t => t.Athletes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training is null) return Results.NotFound("Training not found");

            var athlete = training.Athletes.FirstOrDefault(a => a.Id == userId);
            if (athlete is null) return Results.NotFound("User not registered for this training");

            training.Athletes.Remove(athlete);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("UnregisterAthleteFromTraining")
        .WithOpenApi();

        // POST: api/trainings/{id}/complete
        group.MapPost("/{id:int}/complete", async (int id, ApplicationDbContext db) =>
        {
            var training = await db.Trainings
                .Include(t => t.Athletes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training is null) return Results.NotFound();

            // Award points and XP to all athletes
            foreach (var athlete in training.Athletes)
            {
                athlete.Points += training.Points;
                athlete.Xp += training.Points;

                // Check for level up
                var newLevel = await db.Levels
                    .Where(l => l.RequiredXp <= athlete.Xp)
                    .OrderByDescending(l => l.RequiredXp)
                    .FirstOrDefaultAsync();

                if (newLevel != null && newLevel.Id != athlete.LevelId)
                {
                    athlete.LevelId = newLevel.Id;
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { 
                Message = "Training completed", 
                AthletesRewarded = training.Athletes.Count,
                PointsAwarded = training.Points
            });
        })
        .WithName("CompleteTraining")
        .WithOpenApi();
    }
}

public record TrainingRequest(
    string Name,
    string? Description,
    string? Type,
    DateTime Date,
    int DurationMinutes,
    int Points,
    int MaxAthletes,
    int? BoxId,
    int? TrainerId
);

public record TrainingResponse(
    int Id,
    string Name,
    string Description,
    string Type,
    DateTime Date,
    TimeSpan Duration,
    int Points,
    int MaxAthletes,
    bool IsActive,
    int? BoxId,
    string? BoxName,
    int? TrainerId,
    string? TrainerName,
    int AthleteCount,
    DateTime CreatedAt
);

public record TrainingDetailResponse(
    int Id,
    string Name,
    string Description,
    string Type,
    DateTime Date,
    TimeSpan Duration,
    int Points,
    int MaxAthletes,
    bool IsActive,
    int? BoxId,
    string? BoxName,
    int? TrainerId,
    string? TrainerName,
    List<AthleteInfo> Athletes,
    List<TrainingExerciseInfo> Exercises,
    DateTime CreatedAt
);

public record AthleteInfo(int Id, string Name, string Email);

public record TrainingExerciseRequest(
    int ExerciseId,
    string? Group,
    int Sets,
    string? Repetitions,
    string? Weight,
    string? RestTime,
    string? Notes,
    int? Order
);

public record TrainingExerciseInfo(
    int Id,
    int ExerciseId,
    string ExerciseName,
    string Group,
    int Sets,
    string Repetitions,
    string Weight,
    string RestTime,
    string Notes,
    int Order
);
