using System.Net.Http.Json;

namespace FitPlay.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    private const string BaseUrl = "https://localhost:5000/api";

    #region Records
    public record Exercise(int Id, int TeacherId, string Title, string Category, int Difficulty, int BasePoints, int SuggestedDurationMin, bool IsActive);
    public record ExerciseLog(int Id, int ClientId, int ExerciseId, DateTime PerformedAt, int DurationMin, int PointsAwarded, string? Notes);
    
    // Gamification DTOs
    public record UserProgress(
        int UserId,
        int CurrentLevel,
        string LevelLabel,
        int TotalXp,
        int XpForCurrentLevel,
        int XpForNextLevel,
        int XpProgress,
        double ProgressPercent,
        int TotalTrainingsCompleted,
        int CurrentStreak,
        DateTime LastUpdated
    );

    public record TrainingSummary(
        int Id,
        string Name,
        string Description,
        int DurationMin,
        int XpReward,
        int Difficulty,
        string TrainerName,
        int ExerciseCount,
        bool IsCompleted
    );

    public record TrainingDetail(
        int Id,
        string Name,
        string Description,
        int DurationMin,
        int XpReward,
        int Difficulty,
        int TrainerId,
        string TrainerName,
        bool RequiresValidation,
        bool IsActive,
        List<TrainingExerciseDetail> Exercises
    );

    public record TrainingExerciseDetail(
        int Id,
        int ExerciseId,
        string ExerciseTitle,
        string Category,
        int SortOrder,
        int Sets,
        int Reps,
        int RestSeconds,
        string? Notes
    );

    public record TrainingCompletion(
        int Id,
        int TrainingId,
        string TrainingName,
        int UserId,
        DateTime CompletedAt,
        int XpGranted,
        string Status,
        int? ValidatedByTrainerId,
        DateTime? ValidatedAt,
        string? Notes
    );

    public record CompleteTrainingResponse(
        int CompletionId,
        int XpAwarded,
        string Status,
        int NewTotalXp,
        int NewLevel,
        bool LeveledUp,
        List<Achievement>? NewAchievements
    );

    public record Achievement(
        int Id,
        string AchievementType,
        string Name,
        string Description,
        string? IconUrl,
        DateTime AwardedAt
    );

    public record AchievementStatus(
        string Type,
        string Name,
        string Description,
        bool Earned,
        DateTime? EarnedAt
    );

    public record XpTransaction(
        int Id,
        string TransactionType,
        int XpDelta,
        int XpBefore,
        int XpAfter,
        string? Reason,
        string? AwardedByTrainerName,
        DateTime CreatedAt
    );
    #endregion

    #region Existing Methods
    public async Task<List<Exercise>> GetExercises()
    {
        return await _http.GetFromJsonAsync<List<Exercise>>($"{BaseUrl}/exercises") ?? new();
    }

    public async Task CreateExercise(Exercise exercise)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/exercises", exercise);
        res.EnsureSuccessStatusCode();
    }

    public async Task<int> LogExercise(int clientId, int exerciseId, int duration, string? notes)
    {
        var body = new { ClientId = clientId, ExerciseId = exerciseId, DurationMin = duration, Notes = notes };
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/exerciselogs", body);
        res.EnsureSuccessStatusCode();

        var result = await res.Content.ReadFromJsonAsync<ExerciseLog>();
        return result?.PointsAwarded ?? 0;
    }

    public async Task<List<(int ClientId, int Points)>> GetLeaderboard(string period = "week", int? teacherId = null)
    {
        var url = $"{BaseUrl}/leaderboards?period={period}" + (teacherId.HasValue ? $"&teacherId={teacherId.Value}" : "");
        var data = await _http.GetFromJsonAsync<List<Dictionary<string, int>>>(url);
        return data?.Select(x => (x["ClientId"], x["Points"])).ToList() ?? new();
    }
    #endregion

    #region Progress API
    public async Task<UserProgress?> GetUserProgress(int userId)
    {
        return await _http.GetFromJsonAsync<UserProgress>($"{BaseUrl}/progress/{userId}");
    }

    public async Task<List<XpTransaction>> GetXpHistory(int userId, int limit = 50)
    {
        return await _http.GetFromJsonAsync<List<XpTransaction>>($"{BaseUrl}/progress/{userId}/history?limit={limit}") ?? new();
    }

    public async Task<UserProgress?> AwardBonusXp(int userId, int xpAmount, string reason, int trainerId)
    {
        var body = new { UserId = userId, XpAmount = xpAmount, Reason = reason };
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/progress/bonus?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }

    public async Task<UserProgress?> ResetXp(int userId, string reason, int? newXpValue, int trainerId)
    {
        var body = new { UserId = userId, Reason = reason, NewXpValue = newXpValue };
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/progress/reset?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }
    #endregion

    #region Trainings API
    public async Task<List<TrainingSummary>> GetTrainings(int? userId = null)
    {
        var url = $"{BaseUrl}/v2/trainings" + (userId.HasValue ? $"?userId={userId}" : "");
        return await _http.GetFromJsonAsync<List<TrainingSummary>>(url) ?? new();
    }

    public async Task<List<TrainingSummary>> GetTrainerTrainings(int trainerId)
    {
        return await _http.GetFromJsonAsync<List<TrainingSummary>>($"{BaseUrl}/v2/trainings/trainer/{trainerId}") ?? new();
    }

    public async Task<TrainingDetail?> GetTraining(int trainingId)
    {
        return await _http.GetFromJsonAsync<TrainingDetail>($"{BaseUrl}/v2/trainings/{trainingId}");
    }

    public async Task<TrainingDetail?> CreateTraining(object request, int trainerId)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/v2/trainings?trainerId={trainerId}", request);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TrainingDetail>();
    }

    public async Task<TrainingDetail?> UpdateTraining(int trainingId, object request, int trainerId)
    {
        var res = await _http.PutAsJsonAsync($"{BaseUrl}/v2/trainings/{trainingId}?trainerId={trainerId}", request);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<TrainingDetail>();
    }

    public async Task DeleteTraining(int trainingId, int trainerId)
    {
        var res = await _http.DeleteAsync($"{BaseUrl}/v2/trainings/{trainingId}?trainerId={trainerId}");
        res.EnsureSuccessStatusCode();
    }
    #endregion

    #region Completions API
    public async Task<CompleteTrainingResponse?> CompleteTraining(int trainingId, int userId, string? notes = null)
    {
        var body = new { TrainingId = trainingId, Notes = notes };
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/trainingcompletions?userId={userId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }

    public async Task<List<TrainingCompletion>> GetUserCompletions(int userId, int limit = 50)
    {
        return await _http.GetFromJsonAsync<List<TrainingCompletion>>($"{BaseUrl}/trainingcompletions/user/{userId}?limit={limit}") ?? new();
    }

    public async Task<List<TrainingCompletion>> GetPendingValidations(int trainerId)
    {
        return await _http.GetFromJsonAsync<List<TrainingCompletion>>($"{BaseUrl}/trainingcompletions/pending/{trainerId}") ?? new();
    }

    public async Task<CompleteTrainingResponse?> ValidateCompletion(int completionId, bool approved, int trainerId, int? xpAdjustment = null, string? notes = null)
    {
        var body = new { CompletionId = completionId, Approved = approved, XpAdjustment = xpAdjustment, Notes = notes };
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/trainingcompletions/validate?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }
    #endregion

    #region Achievements API
    public async Task<List<Achievement>> GetUserAchievements(int userId)
    {
        return await _http.GetFromJsonAsync<List<Achievement>>($"{BaseUrl}/achievements/user/{userId}") ?? new();
    }

    public async Task<List<AchievementStatus>> GetAllAchievementsStatus(int userId)
    {
        return await _http.GetFromJsonAsync<List<AchievementStatus>>($"{BaseUrl}/achievements/user/{userId}/all") ?? new();
    }
    #endregion
}
