namespace FitPlay.Domain.DTOs;

public record RoomCheckInResponseDto(
    int Id,
    int ClassEnrollmentId,
    string UserId,
    DateTime CheckInTime,
    int XpAwarded
);

public record CreateRoomCheckInRequest(
    string? DeviceInfo
);
