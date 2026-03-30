using FitPlay.Domain.DTOs;

namespace FitPlay.Domain.Services;

public interface IAcademyService
{
    Task<List<GymResponseDto>> GetGymsAsync(bool? isActive = null);
    Task<GymResponseDto?> GetGymByIdAsync(int gymId);
    Task<GymResponseDto?> GetMyGymAsync(string ownerUserId);
    Task<GymResponseDto> CreateGymAsync(CreateGymRequest request, string? ownerUserId = null);
    Task<GymResponseDto?> UpdateGymAsync(int gymId, UpdateGymRequest request);
    Task<bool> DeactivateGymAsync(int gymId);

    Task<List<GymLocationResponseDto>> GetGymLocationsAsync(int gymId, bool? isActive = null);
    Task<GymLocationResponseDto> CreateGymLocationAsync(CreateGymLocationRequest request);
    Task<GymLocationResponseDto?> UpdateGymLocationAsync(int gymId, int locationId, UpdateGymLocationRequest request);
    Task<bool> DeleteGymLocationAsync(int gymId, int locationId);

    Task<List<TrainerGymLinkResponseDto>> GetGymTrainerLinksAsync(int gymId);
    Task<TrainerGymLinkResponseDto> LinkTrainerAsync(CreateTrainerGymLinkRequest request);
    Task<TrainerGymLinkResponseDto?> UpdateTrainerLinkStatusAsync(int linkId, UpdateTrainerGymLinkStatusRequest request);
    Task<bool> IsTrainerLinkedToGymAsync(string trainerId, int gymId);
}

