using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface ICheckInService
{
    Task<RoomCheckInResponseDto> CheckInAsync(int enrollmentId, string userId, CreateRoomCheckInRequest request);
    Task<List<RoomCheckInResponseDto>> GetSessionCheckInsAsync(int sessionId);
}
