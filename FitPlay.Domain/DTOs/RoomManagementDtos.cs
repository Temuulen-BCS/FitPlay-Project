using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record RoomResponseDto(
    int Id,
    int GymLocationId,
    string Name,
    string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive,
    List<RoomOperatingHoursDto> OperatingHours
);

public record RoomOperatingHoursDto(
    DayOfWeek DayOfWeek,
    TimeOnly? OpenTime,
    TimeOnly? CloseTime,
    bool IsClosed
);

public record CreateRoomRequest(
    int GymLocationId,
    [Required][MaxLength(120)] string Name,
    [MaxLength(500)] string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive = true,
    List<RoomOperatingHoursDto>? OperatingHours = null
);

public record UpdateRoomRequest(
    [Required][MaxLength(120)] string Name,
    [MaxLength(500)] string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive,
    List<RoomOperatingHoursDto>? OperatingHours = null
);

public record RoomBookingResponseDto(
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

public record CreateRoomBookingRequest(
    [Required][MaxLength(100)] string Modality,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);

public record UpdateRoomBookingRequest(
    [Required][MaxLength(100)] string Modality,
    DateTime StartTime,
    DateTime EndTime,
    [Required] string Status,
    string? Notes
);

public record RoomAvailabilitySlotDto(
    DateTime StartTime,
    DateTime EndTime,
    bool IsAvailable,
    int? BookingId
);

public record RoomAvailabilityResponseDto(
    int RoomId,
    DateOnly Date,
    List<RoomAvailabilitySlotDto> Slots
);

public record AvailableRoomsQueryDto(
    int GymLocationId,
    DateTime StartTime,
    DateTime EndTime
);
