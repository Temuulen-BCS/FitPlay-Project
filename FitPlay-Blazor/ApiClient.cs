using System.Net.Http.Json;

namespace FitPlay.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    private const string BaseUrl = "https://localhost:5000/api";

    public record Exercise(int Id, int TeacherId, string Title, string Category, int Difficulty, int BasePoints, int SuggestedDurationMin, bool IsActive);
    public record ExerciseLog(int Id, int ClientId, int ExerciseId, DateTime PerformedAt, int DurationMin, int PointsAwarded, string? Notes);

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
}
