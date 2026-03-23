using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface IRoomService
{
    Task<List<RoomResponseDto>> GetRoomsByLocationAsync(int locationId, bool? isActive = null);
    Task<RoomResponseDto?> GetRoomByIdAsync(int roomId);
    Task<RoomResponseDto> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomResponseDto?> UpdateRoomAsync(int roomId, UpdateRoomRequest request);
    Task<bool> DeleteRoomAsync(int roomId);

    Task<RoomAvailabilityResponseDto> GetRoomAvailabilityAsync(int roomId, DateOnly date);
    Task<List<RoomBookingResponseDto>> GetRoomBookingsAsync(int roomId, DateTime? from = null, DateTime? to = null);

    Task<RoomBookingResponseDto> CreateBookingAsync(int roomId, string trainerId, CreateRoomBookingRequest request);
    Task<RoomBookingResponseDto?> UpdateBookingAsync(int bookingId, string actorUserId, bool isAdmin, UpdateRoomBookingRequest request);
    Task<bool> CancelBookingAsync(int bookingId, string actorUserId, bool isAdmin);

    Task<List<RoomResponseDto>> GetAvailableRoomsAsync(AvailableRoomsQueryDto query);

    Task<List<RoomBookingResponseDto>> GetTrainerBookingsAsync(string trainerId, DateTime? from = null, DateTime? to = null);
    Task<RoomBookingResponseDto?> ConfirmBookingAsync(int bookingId, string actorUserId, bool isAdmin);
}

