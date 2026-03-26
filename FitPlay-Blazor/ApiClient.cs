using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using FitPlay_Blazor.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitPlay.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ApiTokenHandler _tokenHandler;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        HttpClient http,
        AuthenticationStateProvider authStateProvider,
        ApiTokenHandler tokenHandler,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiClient> logger)
    {
        _http = http;
        _authStateProvider = authStateProvider;
        _tokenHandler = tokenHandler;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    #region Auth-aware HTTP wrappers

    /// <summary>
    /// Resolves the current user's ClaimsPrincipal and creates a fresh JWT
    /// Bearer token for the API call.
    /// Works during both SSR (HttpContext) and SignalR circuit
    /// (AuthenticationStateProvider).
    /// Returns the token string, or null if no authenticated user is available.
    /// </summary>
    private async Task<string?> ResolveTokenAsync()
    {
        ClaimsPrincipal? principal = null;
        string authPath = "none";

        // 1) During SSR / initial HTTP request, HttpContext is available
        var httpContextUser = _httpContextAccessor.HttpContext?.User;
        if (httpContextUser?.Identity?.IsAuthenticated == true)
        {
            principal = httpContextUser;
            authPath = "HttpContext";
        }

        // 2) During SignalR circuit, fall back to AuthenticationStateProvider
        if (principal == null)
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User?.Identity?.IsAuthenticated == true)
                {
                    principal = authState.User;
                    authPath = "AuthStateProvider";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get auth state from AuthenticationStateProvider.");
            }
        }

        if (principal != null)
        {
            try
            {
                var token = _tokenHandler.CreateToken(principal);
                _logger.LogDebug("JWT created via {AuthPath} with {ClaimCount} claims.",
                    authPath, principal.Claims.Count());
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create JWT from principal ({AuthPath}).", authPath);
                return null;
            }
        }

        _logger.LogDebug("No authenticated principal available for API call.");
        return null;
    }

    /// <summary>
    /// Builds an HttpRequestMessage with the per-request Authorization header.
    /// This avoids mutating the shared DefaultRequestHeaders on HttpClient,
    /// which is NOT thread-safe in Blazor Server (multiple circuits share one HttpClient instance).
    /// </summary>
    private async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var msg = new HttpRequestMessage(method, url);
        if (content != null)
            msg.Content = content;

        var token = await ResolveTokenAsync();
        if (token != null)
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return msg;
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        var msg = await BuildRequestAsync(HttpMethod.Get, url);
        var res = await _http.SendAsync(msg);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<T>();
    }

    private async Task<HttpResponseMessage> GetAsync(string url)
    {
        var msg = await BuildRequestAsync(HttpMethod.Get, url);
        return await _http.SendAsync(msg);
    }

    private async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T body)
    {
        var content = JsonContent.Create(body);
        var msg = await BuildRequestAsync(HttpMethod.Post, url, content);
        return await _http.SendAsync(msg);
    }

    private async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content)
    {
        var msg = await BuildRequestAsync(HttpMethod.Post, url, content);
        return await _http.SendAsync(msg);
    }

    private async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T body)
    {
        var content = JsonContent.Create(body);
        var msg = await BuildRequestAsync(HttpMethod.Put, url, content);
        return await _http.SendAsync(msg);
    }

    private async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var msg = await BuildRequestAsync(HttpMethod.Delete, url);
        return await _http.SendAsync(msg);
    }

    private async Task<HttpResponseMessage> PatchJsonAsync<T>(string url, T body)
    {
        var content = JsonContent.Create(body);
        var msg = await BuildRequestAsync(HttpMethod.Patch, url, content);
        return await _http.SendAsync(msg);
    }

    #endregion



    #region Records
    public record Exercise(int Id, int TeacherId, string Title, string Category, int Difficulty, int BasePoints, int SuggestedDurationMin, bool IsActive);
    public record ExerciseLog(int Id, int ClientId, int ExerciseId, DateTime PerformedAt, int DurationMin, int PointsAwarded, string? Notes);

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

    public record MembershipStatus(bool IsActive, string Status, DateTime? CurrentPeriodEnd);
    public record CreateSubscriptionRequest(string ReturnUrl);
    public record CreateSubscriptionResponse(string ClientSecret);
    public record ApiError(string? Message);

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

    public record ClassSchedule(
        int Id,
        int? UserId,
        int? TrainerId,
        string Modality,
        DateTime ScheduledAt,
        string Status,
        string? Notes,
        string PaymentStatus = "None",
        decimal? PaidAmount = null
    );

    public record ClassScheduleWithTrainer(
        int Id,
        int? TrainerId,
        string TrainerName,
        string Modality,
        DateTime ScheduledAt,
        string Status,
        string? Notes,
        string PaymentStatus = "None",
        decimal? PaidAmount = null,
        string? RoomBookingStatus = null,
        int QueueCount = 0
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

    public record ExerciseLogWithExercise(
        int Id,
        int ClientId,
        int ExerciseId,
        string ExerciseTitle,
        string ExerciseCategory,
        DateTime PerformedAt,
        int DurationMin,
        int PointsAwarded,
        string? Notes
    );

    public record ExerciseLogSummary(int TotalWorkouts, int TotalMinutes, int TotalPoints);

    public record DomainUserRead(int Id, string Name, string Email, string Phone, string? IdentityUserId);
    public record TrainerRead(int Id, string Name, string Email, string Phone, string? IdentityUserId);

    public record GymRead(int Id, string Name, string CNPJ, decimal CommissionRate, decimal CancelFeeRate, string? StripeAccountId, bool IsActive, string? OwnerUserId);
    public record GymLocationRead(int Id, int GymId, string Name, string Address, string City, string State, string ZipCode, double? Latitude, double? Longitude, bool IsActive);
    public record RoomRead(int Id, int GymLocationId, string Name, string? Description, int Capacity, decimal PricePerHour, bool IsActive, List<RoomOperatingHoursDto>? OperatingHours);
    public record RoomOperatingHoursDto(DayOfWeek DayOfWeek, TimeOnly? OpenTime, TimeOnly? CloseTime, bool IsClosed);

    public record RoomBookingRead(
        int Id,
        int RoomId,
        string TrainerId,
        string Modality,
        DateTime StartTime,
        DateTime EndTime,
        string Status,
        decimal TotalCost,
        string? Notes,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string? RoomName = null,
        string? GymName = null,
        string? LocationName = null,
        int QueuedClientsCount = 0
    );

    public record CreateRoomBookingBody(DateTime StartTime, DateTime EndTime, string Modality, string? Notes);

    public record AvailabilitySlot(DateTime StartTime, DateTime EndTime, bool IsAvailable, int? BookingId);
    public record RoomAvailability(int RoomId, DateOnly Date, List<AvailabilitySlot> Slots);

    public record ClassSessionRead(
        int Id,
        int RoomBookingId,
        string TrainerId,
        string Title,
        string? Description,
        int MaxStudents,
        decimal PricePerStudent,
        DateTime StartTime,
        DateTime EndTime,
        string Status,
        int EnrolledStudents,
        string? TrainerName = null,
        string? RoomName = null,
        string? LocationName = null,

        string? BookingStatus = null,
        decimal? BookingCost = null
    );

    public record CreateClassSessionBody(string Title, string? Description, int MaxStudents, decimal PricePerStudent, DateTime StartTime, DateTime EndTime);

    public record ClassEnrollmentRead(
        int Id,
        int ClassSessionId,
        string UserId,
        string Status,
        decimal PaidAmount,
        string? StripePaymentIntentId,
        DateTime EnrolledAt
    );

    public record SessionEnrollmentRead(
        int Id,
        string UserId,
        string Status,
        decimal PaidAmount,
        DateTime EnrolledAt
    );

    public record SessionEnrollmentDetailRead(
        int Id,
        string UserId,
        string? UserName,
        string? UserEmail,
        string? UserPhone,
        string Status,
        decimal PaidAmount,
        DateTime EnrolledAt
    );

    public record RoomCheckInRead(int Id, int ClassEnrollmentId, string UserId, DateTime CheckInTime, int XpAwarded);

    public record UserEnrollmentWithSession(
        int EnrollmentId,
        string EnrollmentStatus,
        decimal PaidAmount,
        DateTime EnrolledAt,
        int SessionId,
        string SessionTitle,
        string? SessionDescription,
        DateTime StartTime,
        DateTime EndTime,
        decimal PricePerStudent,
        string SessionStatus,
        int MaxStudents,
        int EnrolledStudents,
        bool CheckedIn
    );

    public record TrainerEarningsItem(int ClassSessionId, string SessionTitle, DateTime SessionDate, decimal Amount, DateTime ProcessedAt);
    public record TrainerEarningsSummary(string TrainerId, decimal TotalAmount, List<TrainerEarningsItem> Items);

    // ── GymAdmin records ──
    public record CreateGymRequest(string Name, string CNPJ, decimal CommissionRate, decimal CancelFeeRate);
    public record UpdateGymRequest(string Name, decimal CommissionRate, decimal CancelFeeRate, bool IsActive);
    public record CreateGymLocationRequest(string Name, string Address, string City, string State, string ZipCode, double? Latitude, double? Longitude);
    public record UpdateGymLocationRequest(string Name, string Address, string City, string State, string ZipCode, double? Latitude, double? Longitude, bool IsActive);
    public record CreateRoomRequest(string Name, string? Description, int Capacity, decimal PricePerHour, List<RoomOperatingHoursDto>? OperatingHours = null);
    public record UpdateRoomRequest(string Name, string? Description, int Capacity, decimal PricePerHour, bool IsActive, List<RoomOperatingHoursDto>? OperatingHours = null);
    public record TrainerLinkRead(int Id, string TrainerId, string TrainerName, string TrainerEmail, int GymId, string Status, DateTime CreatedAt);
    public record UpdateTrainerLinkStatusRequest(string Status);

    // Trainer's own gym link records
    public record TrainerGymLinkSelf(int Id, string TrainerId, string GymName, int GymId, string Status, DateTime CreatedAt);
    public record CancellationPreview(decimal CancelFeeRate, decimal FeeAmount);
    public record CreateBookingPaymentIntentResponse(string ClientSecret);
    public record ConfirmBookingPaymentRequest(string StripePaymentIntentId);

    // Class schedule payment records
    public record CreateClassPaymentIntentResponse(string ClientSecret, decimal Amount, string Currency);
    public record ConfirmClassPaymentRequest(int UserId, string StripePaymentIntentId);

    // Dev records
    public record DevScheduleItem(int Id, int? UserId, string? UserName, string Modality, DateTime ScheduledAt, string Status, string? TrainerName);
    public record DevEnrollmentItem(int Id, int ClassSessionId, string UserId, string? UserName, string SessionTitle, DateTime StartTime, DateTime EndTime, string Status);
    public record DevCompleteResponse(bool Success, string Message, int XpAwarded);

    // Queue records
    public record JoinQueueResponse(int QueueEntryId, decimal QueueCost, bool HasMembership, string? ClientSecret, int MonthlySkipCount = 0);
    public record ConfirmQueuePaymentRequest(string StripePaymentIntentId);
    public record UserQueueEntry(int ClassScheduleId, bool IsNotified, decimal QueueCost, bool IsSkipped = false);
    #endregion

    #region Billing API
    public async Task<MembershipStatus?> GetMembershipStatus()
    {
        var res = await GetAsync("/api/billing/status");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<MembershipStatus>();
    }

    public async Task<CreateSubscriptionResponse?> CreateMembershipSubscription(CreateSubscriptionRequest request)
    {
        var res = await PostJsonAsync("/api/billing/create-subscription", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<CreateSubscriptionResponse>();
    }

    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            if (!string.IsNullOrWhiteSpace(error?.Message)) return error.Message;
        }
        catch { }

        var statusCode = (int)response.StatusCode;
        var reason = response.ReasonPhrase;

        // Include diagnostic info for auth failures
        if (statusCode == 401 || statusCode == 403)
        {
            var hadToken = response.RequestMessage?.Headers?.Authorization != null;

            // The API's JwtBearerEvents writes the exact validation error here
            var validationError = response.Headers.TryGetValues("Token-Validation-Error", out var vals)
                ? string.Join("; ", vals)
                : null;

            var detail = statusCode == 401
                ? "The token may be invalid, expired, or the JWT signing key may differ between services."
                : "The user may lack the required role or policy claim.";

            return $"API returned {statusCode} {reason}. "
                 + $"Bearer token was {(hadToken ? "attached" : "NOT attached")} to request. "
                 + (validationError != null ? $"Validation error: {validationError}" : detail);
        }

        return $"Request failed with {statusCode} {reason}.";
    }
    #endregion

    #region Existing Methods
    public async Task<List<Exercise>> GetExercises() => await GetJsonAsync<List<Exercise>>($"/api/exercises") ?? new();

    public async Task CreateExercise(Exercise exercise)
    {
        var res = await PostJsonAsync($"/api/exercises", exercise);
        res.EnsureSuccessStatusCode();
    }

    public async Task<int> LogExercise(int clientId, int exerciseId, int duration, string? notes)
    {
        var body = new { ClientId = clientId, ExerciseId = exerciseId, DurationMin = duration, Notes = notes };
        var res = await PostJsonAsync($"/api/exerciselogs", body);
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<ExerciseLog>();
        return result?.PointsAwarded ?? 0;
    }
    #endregion

    #region Progress API
    public async Task<UserProgress?> GetUserProgress(int userId) => await GetJsonAsync<UserProgress>($"/api/progress/{userId}");
    public async Task<List<XpTransaction>> GetXpHistory(int userId, int limit = 50) => await GetJsonAsync<List<XpTransaction>>($"/api/progress/{userId}/history?limit={limit}") ?? new();

    public async Task<UserProgress?> AwardBonusXp(int userId, int xpAmount, string reason, int trainerId)
    {
        var body = new { UserId = userId, XpAmount = xpAmount, Reason = reason };
        var res = await PostJsonAsync($"/api/progress/bonus?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }

    public async Task<UserProgress?> ResetXp(int userId, string reason, int? newXpValue, int trainerId)
    {
        var body = new { UserId = userId, Reason = reason, NewXpValue = newXpValue };
        var res = await PostJsonAsync($"/api/progress/reset?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }
    #endregion

    #region Trainings API
    public async Task<List<TrainingSummary>> GetTrainings(int? userId = null)
    {
        var url = $"/api/v2/trainings" + (userId.HasValue ? $"?userId={userId}" : "");
        return await GetJsonAsync<List<TrainingSummary>>(url) ?? new();
    }

    public async Task<TrainingDetail?> GetTraining(int trainingId) => await GetJsonAsync<TrainingDetail>($"/api/v2/trainings/{trainingId}");

    public async Task<List<TrainingSummary>> GetTrainerTrainings(int trainerId)
        => await GetJsonAsync<List<TrainingSummary>>($"/api/v2/trainings/trainer/{trainerId}") ?? new();

    public async Task CreateTraining(object request, int trainerId)
    {
        var res = await PostJsonAsync($"/api/v2/trainings?trainerId={trainerId}", request);
        res.EnsureSuccessStatusCode();
    }
    #endregion

    #region Completions API
    public async Task<CompleteTrainingResponse?> CompleteTraining(int trainingId, int userId, string? notes = null)
    {
        var body = new { TrainingId = trainingId, Notes = notes };
        var res = await PostJsonAsync($"/api/trainingcompletions?userId={userId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }

    public async Task<List<TrainingCompletion>> GetUserCompletions(int userId, int limit = 50) => await GetJsonAsync<List<TrainingCompletion>>($"/api/trainingcompletions/user/{userId}?limit={limit}") ?? new();
    public async Task<List<TrainingCompletion>> GetPendingValidations(int trainerId) => await GetJsonAsync<List<TrainingCompletion>>($"/api/trainingcompletions/pending/{trainerId}") ?? new();

    public async Task<CompleteTrainingResponse?> ValidateCompletion(int completionId, bool approved, int trainerId, int? xpAdjustment = null, string? notes = null)
    {
        var body = new { CompletionId = completionId, Approved = approved, XpAdjustment = xpAdjustment, Notes = notes };
        var res = await PostJsonAsync($"/api/trainingcompletions/validate?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }
    #endregion

    #region Achievements API
    public async Task<List<Achievement>> GetUserAchievements(int userId) => await GetJsonAsync<List<Achievement>>($"/api/achievements/user/{userId}") ?? new();
    public async Task<List<AchievementStatus>> GetAllAchievementsStatus(int userId) => await GetJsonAsync<List<AchievementStatus>>($"/api/achievements/user/{userId}/all") ?? new();
    #endregion

    #region Users/Teachers API
    public async Task<DomainUserRead?> GetUserByIdentity(string identityUserId) => await GetJsonAsync<DomainUserRead>($"/api/users/by-identity/{identityUserId}");
    public async Task<TrainerRead?> GetTrainerByIdentity(string identityUserId) => await GetJsonAsync<TrainerRead>($"/api/teachers/by-identity/{identityUserId}");
    public async Task<List<DomainUserRead>> GetUsers() => await GetJsonAsync<List<DomainUserRead>>($"/api/users") ?? new();
    public async Task<List<TrainerRead>> GetTeachers() => await GetJsonAsync<List<TrainerRead>>($"/api/teachers") ?? new();
    #endregion

    #region Schedules API
    public async Task<List<ClassSchedule>> GetUserSchedule(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassSchedule>>($"/api/classeschedules/user/{userId}{qs}") ?? new();
    }

    public async Task<List<ClassScheduleWithTrainer>> GetUserScheduleWithTrainer(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassScheduleWithTrainer>>($"/api/classeschedules/user/{userId}{qs}") ?? new();
    }

    public async Task<List<ClassSchedule>> GetTrainerSchedule(int trainerId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassSchedule>>($"/api/classeschedules/trainer/{trainerId}{qs}") ?? new();
    }

    public async Task<List<ClassScheduleWithTrainer>> GetPublicClassSchedules(DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassScheduleWithTrainer>>($"/api/classeschedules/public{qs}") ?? new();
    }

    public async Task<ClassSchedule?> BookClass(int scheduleId, int userId)
    {
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/book", new { UserId = userId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<ClassSchedule?> UnbookClass(int scheduleId)
    {
        var res = await PostAsync($"/api/classeschedules/{scheduleId}/unbook", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<CreateClassPaymentIntentResponse?> CreateClassBookingPaymentIntent(int scheduleId, int userId)
    {
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/create-payment-intent", new { UserId = userId });
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<CreateClassPaymentIntentResponse>();
    }

    public async Task<ClassSchedule?> ConfirmClassBookingPayment(int scheduleId, int userId, string paymentIntentId)
    {
        var body = new ConfirmClassPaymentRequest(userId, paymentIntentId);
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/confirm-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<ClassSchedule?> UpdateClassSchedule(int scheduleId, string modality, DateTime scheduledAt, string status, string? notes)
    {
        var body = new { Modality = modality, ScheduledAt = scheduledAt, Status = status, Notes = notes };
        var res = await PutJsonAsync($"/api/classeschedules/{scheduleId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<ClassSchedule?> CreateTrainerClassSchedule(int trainerId, string modality, DateTime scheduledAt, string? notes, int? roomBookingId = null)
    {
        var body = new
        {
            UserId = (int?)null,
            TrainerId = trainerId,
            Modality = modality,
            ScheduledAt = scheduledAt,
            Notes = notes,
            RoomBookingId = roomBookingId
        };
        var res = await PostJsonAsync($"/api/classeschedules", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }
    #endregion

    #region Queue API
    public async Task<JoinQueueResponse?> JoinClassQueue(int scheduleId, int userId)
    {
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/join-queue", new { UserId = userId });
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<JoinQueueResponse>();
    }

    public async Task ConfirmQueuePayment(int scheduleId, string paymentIntentId)
    {
        var body = new ConfirmQueuePaymentRequest(paymentIntentId);
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/confirm-queue-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<List<UserQueueEntry>> GetUserQueueEntries(int userId)
        => await GetJsonAsync<List<UserQueueEntry>>($"/api/classeschedules/user/{userId}/queued-classes") ?? new();

    public async Task<int> GetMonthlySkipCount(int userId)
    {
        var result = await GetJsonAsync<int>($"/api/classeschedules/user/{userId}/monthly-skip-count");
        return result;
    }

    public async Task SkipQueueEntry(int scheduleId, int userId)
    {
        var res = await PostJsonAsync($"/api/classeschedules/{scheduleId}/skip-queue", new { UserId = userId });
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }
    #endregion

    #region Exercise Logs API
    public async Task<List<ExerciseLogWithExercise>> GetUserExerciseLogs(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ExerciseLogWithExercise>>($"/api/exerciselogs/user/{userId}{qs}") ?? new();
    }

    public async Task<ExerciseLogSummary?> GetUserExerciseSummary(int userId) => await GetJsonAsync<ExerciseLogSummary>($"/api/exerciselogs/user/{userId}/summary");
    #endregion

    #region Gym/Room/Sessions API
    public async Task<List<GymRead>> GetGyms(bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/academies?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/academies";
        return await GetJsonAsync<List<GymRead>>(url) ?? new();
    }

    public async Task<List<GymLocationRead>> GetGymLocations(int gymId, bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/academies/{gymId}/locations?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/academies/{gymId}/locations";
        return await GetJsonAsync<List<GymLocationRead>>(url) ?? new();
    }

    public async Task<List<RoomRead>> GetRoomsByLocation(int locationId, bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/locations/{locationId}/rooms?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/locations/{locationId}/rooms";
        return await GetJsonAsync<List<RoomRead>>(url) ?? new();
    }

    public async Task<RoomAvailability?> GetRoomAvailability(int roomId, DateOnly date) => await GetJsonAsync<RoomAvailability>($"/api/rooms/{roomId}/availability?date={date:yyyy-MM-dd}");

    public async Task<RoomBookingRead?> CreateRoomBooking(int roomId, CreateRoomBookingBody body)
    {
        var res = await PostJsonAsync($"/api/rooms/{roomId}/bookings", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<ClassSessionRead?> CreateSessionFromBooking(int bookingId, CreateClassSessionBody body)
    {
        var res = await PostJsonAsync($"/api/bookings/{bookingId}/sessions", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSessionRead>();
    }

    public async Task<ClassSessionRead?> GetSession(int sessionId) => await GetJsonAsync<ClassSessionRead>($"/api/sessions/{sessionId}");

    public async Task<List<SessionEnrollmentRead>> GetSessionEnrollments(int sessionId)
        => await GetJsonAsync<List<SessionEnrollmentRead>>($"/api/sessions/{sessionId}/enrollments") ?? new();

    public async Task<List<SessionEnrollmentDetailRead>> GetSessionEnrollmentDetails(int sessionId)
        => await GetJsonAsync<List<SessionEnrollmentDetailRead>>($"/api/sessions/{sessionId}/enrollments/details") ?? new();

    public async Task<ClassEnrollmentRead?> EnrollSession(int sessionId)
    {
        var res = await PostJsonAsync($"/api/sessions/{sessionId}/enroll", new { Notes = (string?)null });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassEnrollmentRead>();
    }

    public async Task<RoomCheckInRead?> CheckInEnrollment(int enrollmentId)
    {
        var res = await PostJsonAsync($"/api/enrollments/{enrollmentId}/checkin", new { DeviceInfo = "blazor" });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<RoomCheckInRead>();
    }

    public async Task<List<UserEnrollmentWithSession>> GetMyEnrollments()
        => await GetJsonAsync<List<UserEnrollmentWithSession>>($"/api/enrollments/mine") ?? new();

    public async Task CancelEnrollment(int enrollmentId)
    {
        var res = await DeleteAsync($"/api/enrollments/{enrollmentId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<CancellationPreview?> GetCancellationPreview(int bookingId)
        => await GetJsonAsync<CancellationPreview>($"/api/bookings/{bookingId}/cancel-preview");

    public async Task CancelRoomBooking(int bookingId)
    {
        var res = await DeleteAsync($"/api/bookings/{bookingId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<List<RoomBookingRead>> GetMyBookings(DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<RoomBookingRead>>($"/api/bookings/mine{qs}") ?? new();
    }

    public async Task<RoomBookingRead?> ConfirmBooking(int bookingId)
    {
        var res = await PostAsync($"/api/bookings/{bookingId}/confirm", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<CreateBookingPaymentIntentResponse?> CreateBookingPaymentIntent(int bookingId)
    {
        var res = await PostAsync($"/api/bookings/{bookingId}/create-payment-intent", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<CreateBookingPaymentIntentResponse>();
    }

    public async Task<RoomBookingRead?> ConfirmBookingPayment(int bookingId, string paymentIntentId)
    {
        var body = new ConfirmBookingPaymentRequest(paymentIntentId);
        var res = await PostJsonAsync($"/api/bookings/{bookingId}/confirm-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<List<ClassSessionRead>> GetTrainerSessionsV3(string trainerIdentityId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassSessionRead>>($"/api/trainers/{trainerIdentityId}/sessions{qs}") ?? new();
    }

    public async Task<List<ClassSessionRead>> GetTrainerCompletedSchedules(string trainerIdentityId)
        => await GetJsonAsync<List<ClassSessionRead>>($"/api/trainers/{trainerIdentityId}/completed-schedules") ?? new();

    public async Task<TrainerEarningsSummary?> GetTrainerEarningsV3(string trainerIdentityId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<TrainerEarningsSummary>($"/api/trainers/{trainerIdentityId}/earnings{qs}");
    }
    #endregion

    #region Registration - Domain Records
    public async Task<DomainUserRead?> RegisterDomainUser(string name, string email, string phone, string identityUserId)
    {
        var body = new { Name = name, Email = email, Phone = phone, IdentityUserId = identityUserId };
        var res = await PostJsonAsync($"/api/users", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DomainUserRead>();
    }

    public async Task<DomainUserRead?> RegisterDomainTrainer(string name, string email, string phone, string identityUserId)
    {
        var body = new { Name = name, Email = email, Phone = phone, IdentityUserId = identityUserId };
        var res = await PostJsonAsync($"/api/teachers", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DomainUserRead>();
    }
    #endregion

    #region GymAdmin API
    // Gyms
    public async Task<GymRead?> GetMyGym()
    {
        var res = await GetAsync($"/api/academies/mine");
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> CreateGym(CreateGymRequest request)
    {
        var res = await PostJsonAsync($"/api/academies", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> UpdateGym(int gymId, UpdateGymRequest request)
    {
        var res = await PutJsonAsync($"/api/academies/{gymId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> GetGym(int gymId)
        => await GetJsonAsync<GymRead>($"/api/academies/{gymId}");

    // Locations
    public async Task<GymLocationRead?> CreateGymLocation(int gymId, CreateGymLocationRequest request)
    {
        var res = await PostJsonAsync($"/api/academies/{gymId}/locations", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymLocationRead>();
    }

    public async Task<GymLocationRead?> UpdateGymLocation(int gymId, int locationId, UpdateGymLocationRequest request)
    {
        var res = await PutJsonAsync($"/api/academies/{gymId}/locations/{locationId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymLocationRead>();
    }

    public async Task DeleteGymLocation(int gymId, int locationId)
    {
        var res = await DeleteAsync($"/api/academies/{gymId}/locations/{locationId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    // Rooms
    public async Task<RoomRead?> CreateRoom(int locationId, CreateRoomRequest request)
    {
        var res = await PostJsonAsync($"/api/locations/{locationId}/rooms", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomRead>();
    }

    public async Task<RoomRead?> UpdateRoom(int roomId, UpdateRoomRequest request)
    {
        var res = await PutJsonAsync($"/api/rooms/{roomId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomRead>();
    }

    public async Task DeleteRoom(int roomId)
    {
        var res = await DeleteAsync($"/api/rooms/{roomId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    // Trainer links
    public async Task<List<TrainerLinkRead>> GetGymTrainerLinks(int gymId)
        => await GetJsonAsync<List<TrainerLinkRead>>($"/api/academies/{gymId}/trainers") ?? new();

    public async Task<TrainerLinkRead?> UpdateTrainerLinkStatus(int gymId, int linkId, string status)
    {
        var body = new UpdateTrainerLinkStatusRequest(status);
        var res = await PatchJsonAsync($"/api/academies/{gymId}/trainers/{linkId}/status", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<TrainerLinkRead>();
    }

    // Trainer self-service gym links
    public async Task<List<TrainerGymLinkSelf>> GetMyGymLinks()
    {
        var res = await GetAsync($"/api/academies/my-links");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<List<TrainerGymLinkSelf>>() ?? new();
    }

    public async Task<TrainerGymLinkSelf?> RequestGymJoin(int gymId, string trainerId)
    {
        var body = new { TrainerId = trainerId, GymId = gymId };
        var res = await PostJsonAsync($"/api/academies/{gymId}/link-trainer", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<TrainerGymLinkSelf>();
    }

    // Sessions in gym
    public async Task<List<ClassSessionRead>> GetGymSessions(int gymId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<ClassSessionRead>>($"/api/academies/{gymId}/sessions{qs}") ?? new();
    }

    // Room bookings in gym
    public async Task<List<RoomBookingRead>> GetGymBookings(int gymId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await GetJsonAsync<List<RoomBookingRead>>($"/api/academies/{gymId}/bookings{qs}") ?? new();
    }

    public async Task CancelSession(int sessionId)
    {
        var res = await DeleteAsync($"/api/sessions/{sessionId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }
    #endregion

    #region Dev API
    public async Task<List<DevScheduleItem>> GetDevSchedules()
        => await GetJsonAsync<List<DevScheduleItem>>($"/api/dev/schedules") ?? new();

    public async Task<List<DevEnrollmentItem>> GetDevEnrollments()
        => await GetJsonAsync<List<DevEnrollmentItem>>($"/api/dev/enrollments") ?? new();

    public async Task<DevCompleteResponse?> DevCompleteSchedule(int scheduleId)
    {
        var body = new { Id = scheduleId };
        var res = await PostJsonAsync($"/api/dev/complete-schedule", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }

    public async Task<DevCompleteResponse?> DevCompleteEnrollment(int enrollmentId)
    {
        var body = new { Id = enrollmentId };
        var res = await PostJsonAsync($"/api/dev/complete-enrollment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }

    public async Task<List<DevEnrollmentItem>> GetDevCompletedEnrollments()
        => await GetJsonAsync<List<DevEnrollmentItem>>($"/api/dev/completed-enrollments") ?? new();

    public async Task<DevCompleteResponse?> DevResetEnrollment(int enrollmentId)
    {
        var body = new { Id = enrollmentId };
        var res = await PostJsonAsync($"/api/dev/reset-enrollment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }
    #endregion
}
