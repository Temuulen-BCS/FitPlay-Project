using System.Net.Http.Json;

namespace FitPlay.Blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;



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
        var res = await _http.GetAsync("/api/billing/status");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<MembershipStatus>();
    }

    public async Task<CreateSubscriptionResponse?> CreateMembershipSubscription(CreateSubscriptionRequest request)
    {
        var res = await _http.PostAsJsonAsync("/api/billing/create-subscription", request);
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

        return $"Request failed with {(int)response.StatusCode} {response.ReasonPhrase}.";
    }
    #endregion

    #region Existing Methods
    public async Task<List<Exercise>> GetExercises() => await _http.GetFromJsonAsync<List<Exercise>>($"/api/exercises") ?? new();

    public async Task CreateExercise(Exercise exercise)
    {
        var res = await _http.PostAsJsonAsync($"/api/exercises", exercise);
        res.EnsureSuccessStatusCode();
    }

    public async Task<int> LogExercise(int clientId, int exerciseId, int duration, string? notes)
    {
        var body = new { ClientId = clientId, ExerciseId = exerciseId, DurationMin = duration, Notes = notes };
        var res = await _http.PostAsJsonAsync($"/api/exerciselogs", body);
        res.EnsureSuccessStatusCode();
        var result = await res.Content.ReadFromJsonAsync<ExerciseLog>();
        return result?.PointsAwarded ?? 0;
    }
    #endregion

    #region Progress API
    public async Task<UserProgress?> GetUserProgress(int userId) => await _http.GetFromJsonAsync<UserProgress>($"/api/progress/{userId}");
    public async Task<List<XpTransaction>> GetXpHistory(int userId, int limit = 50) => await _http.GetFromJsonAsync<List<XpTransaction>>($"/api/progress/{userId}/history?limit={limit}") ?? new();

    public async Task<UserProgress?> AwardBonusXp(int userId, int xpAmount, string reason, int trainerId)
    {
        var body = new { UserId = userId, XpAmount = xpAmount, Reason = reason };
        var res = await _http.PostAsJsonAsync($"/api/progress/bonus?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }

    public async Task<UserProgress?> ResetXp(int userId, string reason, int? newXpValue, int trainerId)
    {
        var body = new { UserId = userId, Reason = reason, NewXpValue = newXpValue };
        var res = await _http.PostAsJsonAsync($"/api/progress/reset?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserProgress>();
    }
    #endregion

    #region Trainings API
    public async Task<List<TrainingSummary>> GetTrainings(int? userId = null)
    {
        var url = $"/api/v2/trainings" + (userId.HasValue ? $"?userId={userId}" : "");
        return await _http.GetFromJsonAsync<List<TrainingSummary>>(url) ?? new();
    }

    public async Task<TrainingDetail?> GetTraining(int trainingId) => await _http.GetFromJsonAsync<TrainingDetail>($"/api/v2/trainings/{trainingId}");

    public async Task<List<TrainingSummary>> GetTrainerTrainings(int trainerId)
        => await _http.GetFromJsonAsync<List<TrainingSummary>>($"/api/v2/trainings/trainer/{trainerId}") ?? new();

    public async Task CreateTraining(object request, int trainerId)
    {
        var res = await _http.PostAsJsonAsync($"/api/v2/trainings?trainerId={trainerId}", request);
        res.EnsureSuccessStatusCode();
    }
    #endregion

    #region Completions API
    public async Task<CompleteTrainingResponse?> CompleteTraining(int trainingId, int userId, string? notes = null)
    {
        var body = new { TrainingId = trainingId, Notes = notes };
        var res = await _http.PostAsJsonAsync($"/api/trainingcompletions?userId={userId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }

    public async Task<List<TrainingCompletion>> GetUserCompletions(int userId, int limit = 50) => await _http.GetFromJsonAsync<List<TrainingCompletion>>($"/api/trainingcompletions/user/{userId}?limit={limit}") ?? new();
    public async Task<List<TrainingCompletion>> GetPendingValidations(int trainerId) => await _http.GetFromJsonAsync<List<TrainingCompletion>>($"/api/trainingcompletions/pending/{trainerId}") ?? new();

    public async Task<CompleteTrainingResponse?> ValidateCompletion(int completionId, bool approved, int trainerId, int? xpAdjustment = null, string? notes = null)
    {
        var body = new { CompletionId = completionId, Approved = approved, XpAdjustment = xpAdjustment, Notes = notes };
        var res = await _http.PostAsJsonAsync($"/api/trainingcompletions/validate?trainerId={trainerId}", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<CompleteTrainingResponse>();
    }
    #endregion

    #region Achievements API
    public async Task<List<Achievement>> GetUserAchievements(int userId) => await _http.GetFromJsonAsync<List<Achievement>>($"/api/achievements/user/{userId}") ?? new();
    public async Task<List<AchievementStatus>> GetAllAchievementsStatus(int userId) => await _http.GetFromJsonAsync<List<AchievementStatus>>($"/api/achievements/user/{userId}/all") ?? new();
    #endregion

    #region Users/Teachers API
    public async Task<DomainUserRead?> GetUserByIdentity(string identityUserId) => await _http.GetFromJsonAsync<DomainUserRead>($"/api/users/by-identity/{identityUserId}");
    public async Task<TrainerRead?> GetTrainerByIdentity(string identityUserId) => await _http.GetFromJsonAsync<TrainerRead>($"/api/teachers/by-identity/{identityUserId}");
    public async Task<List<DomainUserRead>> GetUsers() => await _http.GetFromJsonAsync<List<DomainUserRead>>($"/api/users") ?? new();
    public async Task<List<TrainerRead>> GetTeachers() => await _http.GetFromJsonAsync<List<TrainerRead>>($"/api/teachers") ?? new();
    #endregion

    #region Schedules API
    public async Task<List<ClassSchedule>> GetUserSchedule(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<ClassSchedule>>($"/api/classeschedules/user/{userId}{qs}") ?? new();
    }

    public async Task<List<ClassScheduleWithTrainer>> GetUserScheduleWithTrainer(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<ClassScheduleWithTrainer>>($"/api/classeschedules/user/{userId}{qs}") ?? new();
    }

    public async Task<List<ClassSchedule>> GetTrainerSchedule(int trainerId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<ClassSchedule>>($"/api/classeschedules/trainer/{trainerId}{qs}") ?? new();
    }

    public async Task<List<ClassScheduleWithTrainer>> GetPublicClassSchedules(DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<ClassScheduleWithTrainer>>($"/api/classeschedules/public{qs}") ?? new();
    }

    public async Task<ClassSchedule?> BookClass(int scheduleId, int userId)
    {
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/book", new { UserId = userId });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<ClassSchedule?> UnbookClass(int scheduleId)
    {
        var res = await _http.PostAsync($"/api/classeschedules/{scheduleId}/unbook", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<CreateClassPaymentIntentResponse?> CreateClassBookingPaymentIntent(int scheduleId, int userId)
    {
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/create-payment-intent", new { UserId = userId });
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<CreateClassPaymentIntentResponse>();
    }

    public async Task<ClassSchedule?> ConfirmClassBookingPayment(int scheduleId, int userId, string paymentIntentId)
    {
        var body = new ConfirmClassPaymentRequest(userId, paymentIntentId);
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/confirm-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }

    public async Task<ClassSchedule?> UpdateClassSchedule(int scheduleId, string modality, DateTime scheduledAt, string status, string? notes)
    {
        var body = new { Modality = modality, ScheduledAt = scheduledAt, Status = status, Notes = notes };
        var res = await _http.PutAsJsonAsync($"/api/classeschedules/{scheduleId}", body);
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
        var res = await _http.PostAsJsonAsync($"/api/classeschedules", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSchedule>();
    }
    #endregion

    #region Queue API
    public async Task<JoinQueueResponse?> JoinClassQueue(int scheduleId, int userId)
    {
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/join-queue", new { UserId = userId });
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<JoinQueueResponse>();
    }

    public async Task ConfirmQueuePayment(int scheduleId, string paymentIntentId)
    {
        var body = new ConfirmQueuePaymentRequest(paymentIntentId);
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/confirm-queue-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<List<UserQueueEntry>> GetUserQueueEntries(int userId)
        => await _http.GetFromJsonAsync<List<UserQueueEntry>>($"/api/classeschedules/user/{userId}/queued-classes") ?? new();

    public async Task<int> GetMonthlySkipCount(int userId)
    {
        var result = await _http.GetFromJsonAsync<int>($"/api/classeschedules/user/{userId}/monthly-skip-count");
        return result;
    }

    public async Task SkipQueueEntry(int scheduleId, int userId)
    {
        var res = await _http.PostAsJsonAsync($"/api/classeschedules/{scheduleId}/skip-queue", new { UserId = userId });
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
        return await _http.GetFromJsonAsync<List<ExerciseLogWithExercise>>($"/api/exerciselogs/user/{userId}{qs}") ?? new();
    }

    public async Task<ExerciseLogSummary?> GetUserExerciseSummary(int userId) => await _http.GetFromJsonAsync<ExerciseLogSummary>($"/api/exerciselogs/user/{userId}/summary");
    #endregion

    #region Gym/Room/Sessions API
    public async Task<List<GymRead>> GetGyms(bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/academies?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/academies";
        return await _http.GetFromJsonAsync<List<GymRead>>(url) ?? new();
    }

    public async Task<List<GymLocationRead>> GetGymLocations(int gymId, bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/academies/{gymId}/locations?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/academies/{gymId}/locations";
        return await _http.GetFromJsonAsync<List<GymLocationRead>>(url) ?? new();
    }

    public async Task<List<RoomRead>> GetRoomsByLocation(int locationId, bool? isActive = null)
    {
        var url = isActive.HasValue ? $"/api/locations/{locationId}/rooms?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"/api/locations/{locationId}/rooms";
        return await _http.GetFromJsonAsync<List<RoomRead>>(url) ?? new();
    }

    public async Task<RoomAvailability?> GetRoomAvailability(int roomId, DateOnly date) => await _http.GetFromJsonAsync<RoomAvailability>($"/api/rooms/{roomId}/availability?date={date:yyyy-MM-dd}");

    public async Task<RoomBookingRead?> CreateRoomBooking(int roomId, CreateRoomBookingBody body)
    {
        var res = await _http.PostAsJsonAsync($"/api/rooms/{roomId}/bookings", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<ClassSessionRead?> CreateSessionFromBooking(int bookingId, CreateClassSessionBody body)
    {
        var res = await _http.PostAsJsonAsync($"/api/bookings/{bookingId}/sessions", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassSessionRead>();
    }

    public async Task<ClassSessionRead?> GetSession(int sessionId) => await _http.GetFromJsonAsync<ClassSessionRead>($"/api/sessions/{sessionId}");

    public async Task<List<SessionEnrollmentRead>> GetSessionEnrollments(int sessionId)
        => await _http.GetFromJsonAsync<List<SessionEnrollmentRead>>($"/api/sessions/{sessionId}/enrollments") ?? new();

    public async Task<List<SessionEnrollmentDetailRead>> GetSessionEnrollmentDetails(int sessionId)
        => await _http.GetFromJsonAsync<List<SessionEnrollmentDetailRead>>($"/api/sessions/{sessionId}/enrollments/details") ?? new();

    public async Task<ClassEnrollmentRead?> EnrollSession(int sessionId)
    {
        var res = await _http.PostAsJsonAsync($"/api/sessions/{sessionId}/enroll", new { Notes = (string?)null });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<ClassEnrollmentRead>();
    }

    public async Task<RoomCheckInRead?> CheckInEnrollment(int enrollmentId)
    {
        var res = await _http.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/checkin", new { DeviceInfo = "blazor" });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<RoomCheckInRead>();
    }

    public async Task<List<UserEnrollmentWithSession>> GetMyEnrollments()
        => await _http.GetFromJsonAsync<List<UserEnrollmentWithSession>>($"/api/enrollments/mine") ?? new();

    public async Task CancelEnrollment(int enrollmentId)
    {
        var res = await _http.DeleteAsync($"/api/enrollments/{enrollmentId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<CancellationPreview?> GetCancellationPreview(int bookingId)
        => await _http.GetFromJsonAsync<CancellationPreview>($"/api/bookings/{bookingId}/cancel-preview");

    public async Task CancelRoomBooking(int bookingId)
    {
        var res = await _http.DeleteAsync($"/api/bookings/{bookingId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    public async Task<List<RoomBookingRead>> GetMyBookings(DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<RoomBookingRead>>($"/api/bookings/mine{qs}") ?? new();
    }

    public async Task<RoomBookingRead?> ConfirmBooking(int bookingId)
    {
        var res = await _http.PostAsync($"/api/bookings/{bookingId}/confirm", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<CreateBookingPaymentIntentResponse?> CreateBookingPaymentIntent(int bookingId)
    {
        var res = await _http.PostAsync($"/api/bookings/{bookingId}/create-payment-intent", null);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<CreateBookingPaymentIntentResponse>();
    }

    public async Task<RoomBookingRead?> ConfirmBookingPayment(int bookingId, string paymentIntentId)
    {
        var body = new ConfirmBookingPaymentRequest(paymentIntentId);
        var res = await _http.PostAsJsonAsync($"/api/bookings/{bookingId}/confirm-payment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomBookingRead>();
    }

    public async Task<List<ClassSessionRead>> GetTrainerSessionsV3(string trainerIdentityId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<ClassSessionRead>>($"/api/trainers/{trainerIdentityId}/sessions{qs}") ?? new();
    }

    public async Task<List<ClassSessionRead>> GetTrainerCompletedSchedules(string trainerIdentityId)
        => await _http.GetFromJsonAsync<List<ClassSessionRead>>($"/api/trainers/{trainerIdentityId}/completed-schedules") ?? new();

    public async Task<TrainerEarningsSummary?> GetTrainerEarningsV3(string trainerIdentityId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<TrainerEarningsSummary>($"/api/trainers/{trainerIdentityId}/earnings{qs}");
    }
    #endregion

    #region Registration - Domain Records
    public async Task<DomainUserRead?> RegisterDomainUser(string name, string email, string phone, string identityUserId)
    {
        var body = new { Name = name, Email = email, Phone = phone, IdentityUserId = identityUserId };
        var res = await _http.PostAsJsonAsync($"/api/users", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DomainUserRead>();
    }

    public async Task<DomainUserRead?> RegisterDomainTrainer(string name, string email, string phone, string identityUserId)
    {
        var body = new { Name = name, Email = email, Phone = phone, IdentityUserId = identityUserId };
        var res = await _http.PostAsJsonAsync($"/api/teachers", body);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<DomainUserRead>();
    }
    #endregion

    #region GymAdmin API
    // Gyms
    public async Task<GymRead?> GetMyGym()
    {
        var res = await _http.GetAsync($"/api/academies/mine");
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> CreateGym(CreateGymRequest request)
    {
        var res = await _http.PostAsJsonAsync($"/api/academies", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> UpdateGym(int gymId, UpdateGymRequest request)
    {
        var res = await _http.PutAsJsonAsync($"/api/academies/{gymId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymRead>();
    }

    public async Task<GymRead?> GetGym(int gymId)
        => await _http.GetFromJsonAsync<GymRead>($"/api/academies/{gymId}");

    // Locations
    public async Task<GymLocationRead?> CreateGymLocation(int gymId, CreateGymLocationRequest request)
    {
        var res = await _http.PostAsJsonAsync($"/api/academies/{gymId}/locations", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymLocationRead>();
    }

    public async Task<GymLocationRead?> UpdateGymLocation(int gymId, int locationId, UpdateGymLocationRequest request)
    {
        var res = await _http.PutAsJsonAsync($"/api/academies/{gymId}/locations/{locationId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<GymLocationRead>();
    }

    public async Task DeleteGymLocation(int gymId, int locationId)
    {
        var res = await _http.DeleteAsync($"/api/academies/{gymId}/locations/{locationId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    // Rooms
    public async Task<RoomRead?> CreateRoom(int locationId, CreateRoomRequest request)
    {
        var res = await _http.PostAsJsonAsync($"/api/locations/{locationId}/rooms", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomRead>();
    }

    public async Task<RoomRead?> UpdateRoom(int roomId, UpdateRoomRequest request)
    {
        var res = await _http.PutAsJsonAsync($"/api/rooms/{roomId}", request);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<RoomRead>();
    }

    public async Task DeleteRoom(int roomId)
    {
        var res = await _http.DeleteAsync($"/api/rooms/{roomId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }

    // Trainer links
    public async Task<List<TrainerLinkRead>> GetGymTrainerLinks(int gymId)
        => await _http.GetFromJsonAsync<List<TrainerLinkRead>>($"/api/academies/{gymId}/trainers") ?? new();

    public async Task<TrainerLinkRead?> UpdateTrainerLinkStatus(int gymId, int linkId, string status)
    {
        var body = new UpdateTrainerLinkStatusRequest(status);
        var res = await _http.PatchAsJsonAsync($"/api/academies/{gymId}/trainers/{linkId}/status", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<TrainerLinkRead>();
    }

    // Trainer self-service gym links
    public async Task<List<TrainerGymLinkSelf>> GetMyGymLinks()
    {
        var res = await _http.GetAsync($"/api/academies/my-links");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<List<TrainerGymLinkSelf>>() ?? new();
    }

    public async Task<TrainerGymLinkSelf?> RequestGymJoin(int gymId, string trainerId)
    {
        var body = new { TrainerId = trainerId, GymId = gymId };
        var res = await _http.PostAsJsonAsync($"/api/academies/{gymId}/link-trainer", body);
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
        return await _http.GetFromJsonAsync<List<ClassSessionRead>>($"/api/academies/{gymId}/sessions{qs}") ?? new();
    }

    // Room bookings in gym
    public async Task<List<RoomBookingRead>> GetGymBookings(int gymId, DateTime? from = null, DateTime? to = null)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return await _http.GetFromJsonAsync<List<RoomBookingRead>>($"/api/academies/{gymId}/bookings{qs}") ?? new();
    }

    public async Task CancelSession(int sessionId)
    {
        var res = await _http.DeleteAsync($"/api/sessions/{sessionId}");
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
    }
    #endregion

    #region Dev API
    public async Task<List<DevScheduleItem>> GetDevSchedules()
        => await _http.GetFromJsonAsync<List<DevScheduleItem>>($"/api/dev/schedules") ?? new();

    public async Task<List<DevEnrollmentItem>> GetDevEnrollments()
        => await _http.GetFromJsonAsync<List<DevEnrollmentItem>>($"/api/dev/enrollments") ?? new();

    public async Task<DevCompleteResponse?> DevCompleteSchedule(int scheduleId)
    {
        var body = new { Id = scheduleId };
        var res = await _http.PostAsJsonAsync($"/api/dev/complete-schedule", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }

    public async Task<DevCompleteResponse?> DevCompleteEnrollment(int enrollmentId)
    {
        var body = new { Id = enrollmentId };
        var res = await _http.PostAsJsonAsync($"/api/dev/complete-enrollment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }

    public async Task<List<DevEnrollmentItem>> GetDevCompletedEnrollments()
        => await _http.GetFromJsonAsync<List<DevEnrollmentItem>>($"/api/dev/completed-enrollments") ?? new();

    public async Task<DevCompleteResponse?> DevResetEnrollment(int enrollmentId)
    {
        var body = new { Id = enrollmentId };
        var res = await _http.PostAsJsonAsync($"/api/dev/reset-enrollment", body);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(await ReadApiErrorAsync(res));
        return await res.Content.ReadFromJsonAsync<DevCompleteResponse>();
    }
    #endregion
}
