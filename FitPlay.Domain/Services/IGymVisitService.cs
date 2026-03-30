using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface IGymVisitService
{
    Task<GymVisitResponseDto> CheckInAsync(string userId, GymCheckInRequest request);
    Task<GymVisitResponseDto> CheckOutAsync(string userId, GymCheckOutRequest request);
    Task<GymVisitResponseDto?> GetActiveVisitAsync(string userId);
    Task<List<GymVisitResponseDto>> GetVisitHistoryAsync(string userId, int limit = 50);
    Task<List<GymLocationForCheckInDto>> GetAllActiveGymLocationsAsync();
    Task<List<LocationPresenceDto>> GetActiveCountsByGymAsync(int gymId);
    Task<List<ActiveVisitDetailDto>> GetActiveVisitDetailsByLocationAsync(int gymLocationId);
    Task<int?> GetGymIdFromLocationIdAsync(int gymLocationId);
    Task<CheckInEligibilityDto> GetCheckInEligibilityAsync(string userId, int gymLocationId);
    Task<List<PastClassDto>> GetPastClassesAsync(string userId, int gymLocationId);
}
