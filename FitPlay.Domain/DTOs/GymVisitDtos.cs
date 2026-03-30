using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record GymVisitResponseDto(
    int Id,
    string UserId,
    int GymLocationId,
    string GymLocationName,
    DateTime CheckInTime,
    DateTime? CheckOutTime,
    double CheckInLatitude,
    double CheckInLongitude,
    double? CheckOutLatitude,
    double? CheckOutLongitude
);

public record GymCheckInRequest(
    int GymLocationId,
    double Latitude,
    double Longitude
);

public record GymCheckOutRequest(
    double Latitude,
    double Longitude
);

public record GymLocationForCheckInDto(
    int Id,
    string Name,
    string Address,
    string City,
    string State,
    double? Latitude,
    double? Longitude
);

public record LocationPresenceDto(
    int GymLocationId,
    string LocationName,
    int ActiveCount
);

public record CheckInEligibilityDto(
    bool HasEnrollment,
    bool CanCheckInNow,
    DateTime? NextClassStartTime,
    DateTime? NextClassEndTime,
    string? NextClassTitle,
    bool HasPastClass = false,
    bool CheckedInForPastClass = false,
    DateTime? PastClassCheckInTime = null,
    string? PastClassTitle = null
);

public record PastClassDto(
    string Title,
    DateTime ClassStartTime,
    DateTime ClassEndTime,
    DateTime? CheckInTime,
    DateTime? CheckOutTime,
    int? DurationMinutes,
    string BookingSource,
    string? RoomName
);

public record ActiveVisitDetailDto(
    int VisitId,
    string UserId,
    string UserName,
    string UserEmail,
    string? UserPhone,
    DateTime CheckInTime,
    // Booking/Session info (null if no active class)
    int? ClassSessionId,
    string? ClassTitle,
    DateTime? SessionStartTime,
    DateTime? SessionEndTime,
    string? TrainerId,
    string? TrainerName,
    string? TrainerEmail,
    string? RoomName,
    decimal? PaidAmount,
    string? EnrollmentStatus
);
