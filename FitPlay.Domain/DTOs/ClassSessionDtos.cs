using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record ClassSessionResponseDto(
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
    string? BookingPurpose = null,
    string? BookingStatus = null,
    decimal? BookingCost = null,
    string? BookingNotes = null
);

public record CreateClassSessionRequest(
    [Required][MaxLength(150)] string Title,
    [MaxLength(1000)] string? Description,
    int MaxStudents,
    decimal PricePerStudent,
    DateTime StartTime,
    DateTime EndTime
);

public record UpdateClassSessionRequest(
    [Required][MaxLength(150)] string Title,
    [MaxLength(1000)] string? Description,
    int MaxStudents,
    decimal PricePerStudent,
    DateTime StartTime,
    DateTime EndTime,
    [Required] string Status
);

public record ClassEnrollmentResponseDto(
    int Id,
    int ClassSessionId,
    string UserId,
    string Status,
    decimal PaidAmount,
    string? StripePaymentIntentId,
    DateTime EnrolledAt
);

public record CreateClassEnrollmentRequest(
    string? Notes
);

public record UpdateClassEnrollmentStatusRequest(
    [Required] string Status
);

public record UserEnrollmentWithSessionDto(
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
