using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

/// <summary>
/// Service for managing trainings (CRUD operations by trainers).
/// </summary>
public class TrainingService
{
    private readonly FitPlayContext _db;

    public TrainingService(FitPlayContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all active trainings.
    /// </summary>
    public async Task<List<TrainingSummaryDto>> GetTrainingsAsync(int? userId = null)
    {
        var query = _db.Trainings
            .Where(t => t.IsActive)
            .Include(t => t.Trainer)
            .Include(t => t.Exercises);

        var trainings = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        // Get completed training IDs for user
        HashSet<int> completedIds = new();
        if (userId.HasValue)
        {
            completedIds = (await _db.TrainingCompletions
                .Where(c => c.UserId == userId.Value && 
                    (c.Status == ValidationStatus.AutoApproved || c.Status == ValidationStatus.Validated))
                .Select(c => c.TrainingId)
                .ToListAsync())
                .ToHashSet();
        }

        return trainings.Select(t => new TrainingSummaryDto(
            t.Id,
            t.Name,
            t.Description,
            t.DurationMin,
            t.XpReward,
            t.Difficulty,
            t.Trainer?.Name ?? "Unknown",
            t.Exercises.Count,
            completedIds.Contains(t.Id)
        )).ToList();
    }

    /// <summary>
    /// Get trainings by a specific trainer.
    /// </summary>
    public async Task<List<TrainingSummaryDto>> GetTrainerTrainingsAsync(int trainerId)
    {
        var trainings = await _db.Trainings
            .Where(t => t.TrainerId == trainerId)
            .Include(t => t.Trainer)
            .Include(t => t.Exercises)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return trainings.Select(t => new TrainingSummaryDto(
            t.Id,
            t.Name,
            t.Description,
            t.DurationMin,
            t.XpReward,
            t.Difficulty,
            t.Trainer?.Name ?? "Unknown",
            t.Exercises.Count,
            false
        )).ToList();
    }

    /// <summary>
    /// Get a training by ID with full details.
    /// </summary>
    public async Task<TrainingDto?> GetTrainingAsync(int trainingId)
    {
        var training = await _db.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Exercises)
                .ThenInclude(te => te.Exercise)
            .FirstOrDefaultAsync(t => t.Id == trainingId);

        if (training == null) return null;

        return new TrainingDto(
            training.Id,
            training.Name,
            training.Description,
            training.DurationMin,
            training.XpReward,
            training.Difficulty,
            training.TrainerId,
            training.Trainer?.Name ?? "Unknown",
            training.RequiresValidation,
            training.IsActive,
            training.Exercises.OrderBy(e => e.SortOrder).Select(e => new TrainingExerciseDto(
                e.Id,
                e.ExerciseId,
                e.Exercise?.Title ?? "Unknown",
                e.Exercise?.Category ?? "",
                e.SortOrder,
                e.Sets,
                e.Reps,
                e.RestSeconds,
                e.Notes
            )).ToList()
        );
    }

    /// <summary>
    /// Create a new training (trainer action).
    /// </summary>
    public async Task<TrainingDto> CreateTrainingAsync(int trainerId, CreateTrainingRequest request)
    {
        var training = new Training
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DurationMin = request.DurationMin,
            XpReward = request.XpReward,
            Difficulty = request.Difficulty,
            TrainerId = trainerId,
            RequiresValidation = request.RequiresValidation
        };

        _db.Trainings.Add(training);
        await _db.SaveChangesAsync();

        // Add exercises
        if (request.Exercises?.Any() == true)
        {
            var sortOrder = 0;
            foreach (var ex in request.Exercises)
            {
                var te = new TrainingExercise
                {
                    TrainingId = training.Id,
                    ExerciseId = ex.ExerciseId,
                    SortOrder = ex.SortOrder > 0 ? ex.SortOrder : sortOrder++,
                    Sets = ex.Sets,
                    Reps = ex.Reps,
                    RestSeconds = ex.RestSeconds,
                    Notes = ex.Notes
                };
                _db.TrainingExercises.Add(te);
            }
            await _db.SaveChangesAsync();
        }

        return (await GetTrainingAsync(training.Id))!;
    }

    /// <summary>
    /// Update a training (trainer action).
    /// </summary>
    public async Task<TrainingDto?> UpdateTrainingAsync(int trainingId, int trainerId, UpdateTrainingRequest request)
    {
        var training = await _db.Trainings.FirstOrDefaultAsync(t => t.Id == trainingId && t.TrainerId == trainerId);
        if (training == null) return null;

        if (request.Name != null) training.Name = request.Name;
        if (request.Description != null) training.Description = request.Description;
        if (request.DurationMin.HasValue) training.DurationMin = request.DurationMin.Value;
        if (request.XpReward.HasValue) training.XpReward = request.XpReward.Value;
        if (request.Difficulty.HasValue) training.Difficulty = request.Difficulty.Value;
        if (request.RequiresValidation.HasValue) training.RequiresValidation = request.RequiresValidation.Value;
        if (request.IsActive.HasValue) training.IsActive = request.IsActive.Value;

        training.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetTrainingAsync(trainingId);
    }

    /// <summary>
    /// Delete a training (trainer action).
    /// </summary>
    public async Task<bool> DeleteTrainingAsync(int trainingId, int trainerId)
    {
        var training = await _db.Trainings.FirstOrDefaultAsync(t => t.Id == trainingId && t.TrainerId == trainerId);
        if (training == null) return false;

        // Soft delete
        training.IsActive = false;
        training.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }
}
