using System.ComponentModel.DataAnnotations;

namespace FitPlay.Domain.DTOs;

public record RoomResponseDto(
    int Id,
    int GymLocationId,
    string Name,
    string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive
);

public record CreateRoomRequest(
    int GymLocationId,
    [Required][MaxLength(120)] string Name,
    [MaxLength(500)] string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive = true
);

public record UpdateRoomRequest(
    [Required][MaxLength(120)] string Name,
    [MaxLength(500)] string? Description,
    int Capacity,
    decimal PricePerHour,
    bool IsActive
);

public record RoomBookingResponseDto(
    int Id,
    int RoomId,
    string TrainerId,
    string Purpose,
    string? PurposeDescription,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    decimal TotalCost,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateRoomBookingRequest(
    [Required] string Purpose,
    string? PurposeDescription,
    DateTime StartTime,
    DateTime EndTime,
    string? Notes
);

public record UpdateRoomBookingRequest(
    [Required] string Purpose,
    string? PurposeDescription,
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
