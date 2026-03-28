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