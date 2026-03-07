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
    int EnrolledStudents
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
