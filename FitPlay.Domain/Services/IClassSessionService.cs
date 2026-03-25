using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface IClassSessionService
{
    Task<ClassSessionResponseDto?> GetSessionByIdAsync(int sessionId);
    Task<List<ClassSessionResponseDto>> GetTrainerSessionsAsync(string trainerId, DateTime? from = null, DateTime? to = null);
    Task<List<ClassSessionResponseDto>> GetSessionsByGymAsync(int gymId, DateTime? from = null, DateTime? to = null);

    Task<ClassSessionResponseDto> CreateSessionAsync(int bookingId, string trainerId, CreateClassSessionRequest request);
    Task<ClassSessionResponseDto?> UpdateSessionAsync(int sessionId, string actorUserId, bool isAdmin, UpdateClassSessionRequest request);
    Task<bool> CancelSessionAsync(int sessionId, string actorUserId, bool isAdmin);

    Task<ClassEnrollmentResponseDto> EnrollAsync(int sessionId, string userId, CreateClassEnrollmentRequest request);
    Task<bool> CancelEnrollmentAsync(int enrollmentId, string actorUserId, bool isAdmin);
    Task<List<UserEnrollmentWithSessionDto>> GetMyEnrollmentsAsync(string userId);
    Task<List<SessionEnrollmentDto>> GetEnrollmentsBySessionAsync(int sessionId);
    Task<List<SessionEnrollmentDetailDto>> GetEnrollmentDetailsBySessionAsync(int sessionId);
}

