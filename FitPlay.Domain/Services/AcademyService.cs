using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class AcademyService : IAcademyService
{
    private readonly FitPlayContext _db;

    public AcademyService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<List<GymResponseDto>> GetGymsAsync(bool? isActive = null)
    {
        var query = _db.Gyms.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(g => g.IsActive == isActive.Value);
        }

        var gyms = await query.OrderBy(g => g.Name).ToListAsync();
        return gyms.Select(ToDto).ToList();
    }

    public async Task<GymResponseDto?> GetGymByIdAsync(int gymId)
    {
        var gym = await _db.Gyms.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gymId);
        return gym is null ? null : ToDto(gym);
    }

    public async Task<GymResponseDto?> GetMyGymAsync(string ownerUserId)
    {
        var gym = await _db.Gyms.AsNoTracking()
            .FirstOrDefaultAsync(g => g.OwnerUserId == ownerUserId);
        return gym is null ? null : ToDto(gym);
    }

    public async Task<GymResponseDto> CreateGymAsync(CreateGymRequest request, string? ownerUserId = null)
    {
        var gym = new Gym
        {
            Name = request.Name.Trim(),
            CNPJ = request.CNPJ.Trim(),
            CommissionRate = request.CommissionRate,
            CancelFeeRate = request.CancelFeeRate,
            StripeAccountId = request.StripeAccountId?.Trim(),
            IsActive = request.IsActive,
            OwnerUserId = ownerUserId
        };

        _db.Gyms.Add(gym);
        await _db.SaveChangesAsync();

        return ToDto(gym);
    }

    public async Task<GymResponseDto?> UpdateGymAsync(int gymId, UpdateGymRequest request)
    {
        var gym = await _db.Gyms.FirstOrDefaultAsync(g => g.Id == gymId);
        if (gym is null) return null;

        gym.Name = request.Name.Trim();
        gym.CNPJ = request.CNPJ.Trim();
        gym.CommissionRate = request.CommissionRate;
        gym.CancelFeeRate = request.CancelFeeRate;
        gym.StripeAccountId = request.StripeAccountId?.Trim();
        gym.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ToDto(gym);
    }

    public async Task<bool> DeactivateGymAsync(int gymId)
    {
        var gym = await _db.Gyms.FirstOrDefaultAsync(g => g.Id == gymId);
        if (gym is null) return false;

        gym.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<GymLocationResponseDto>> GetGymLocationsAsync(int gymId, bool? isActive = null)
    {
        var query = _db.GymLocations.AsNoTracking().Where(gl => gl.GymId == gymId).AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(gl => gl.IsActive == isActive.Value);
        }

        var locations = await query.OrderBy(gl => gl.Name).ToListAsync();
        return locations.Select(ToDto).ToList();
    }

    public async Task<GymLocationResponseDto> CreateGymLocationAsync(CreateGymLocationRequest request)
    {
        var location = new GymLocation
        {
            GymId = request.GymId,
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            ZipCode = request.ZipCode.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = request.IsActive
        };

        _db.GymLocations.Add(location);
        await _db.SaveChangesAsync();

        return ToDto(location);
    }

    public async Task<GymLocationResponseDto?> UpdateGymLocationAsync(int gymId, int locationId, UpdateGymLocationRequest request)
    {
        var location = await _db.GymLocations.FirstOrDefaultAsync(gl => gl.Id == locationId && gl.GymId == gymId);
        if (location is null) return null;

        location.Name = request.Name.Trim();
        location.Address = request.Address.Trim();
        location.City = request.City.Trim();
        location.State = request.State.Trim();
        location.ZipCode = request.ZipCode.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ToDto(location);
    }

    public async Task<TrainerGymLinkResponseDto> LinkTrainerAsync(CreateTrainerGymLinkRequest request)
    {
        var trainerId = request.TrainerId.Trim();

        var existing = await _db.TrainerGymLinks
            .FirstOrDefaultAsync(l => l.TrainerId == trainerId && l.GymId == request.GymId);

        if (existing is not null)
        {
            return ToDto(existing);
        }

        var link = new TrainerGymLink
        {
            TrainerId = trainerId,
            GymId = request.GymId,
            Status = TrainerGymLinkStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.TrainerGymLinks.Add(link);
        await _db.SaveChangesAsync();

        return ToDto(link);
    }

    public async Task<TrainerGymLinkResponseDto?> UpdateTrainerLinkStatusAsync(int linkId, UpdateTrainerGymLinkStatusRequest request)
    {
        var link = await _db.TrainerGymLinks.FirstOrDefaultAsync(l => l.Id == linkId);
        if (link is null) return null;

        if (!Enum.TryParse<TrainerGymLinkStatus>(request.Status, true, out var parsedStatus))
        {
            throw new ArgumentException("Invalid trainer-gym link status.");
        }

        link.Status = parsedStatus;
        await _db.SaveChangesAsync();

        return ToDto(link);
    }

    public async Task<bool> DeleteGymLocationAsync(int gymId, int locationId)
    {
        var location = await _db.GymLocations.FirstOrDefaultAsync(gl => gl.Id == locationId && gl.GymId == gymId);
        if (location is null) return false;
        _db.GymLocations.Remove(location);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TrainerGymLinkResponseDto>> GetGymTrainerLinksAsync(int gymId)
    {
        var links = await _db.TrainerGymLinks
            .AsNoTracking()
            .Where(l => l.GymId == gymId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        var trainerIds = links.Select(l => l.TrainerId).Distinct().ToList();
        var teachers = await _db.Teachers
            .AsNoTracking()
            .Where(t => t.IdentityUserId != null && trainerIds.Contains(t.IdentityUserId))
            .ToListAsync();

        var teacherByIdentity = teachers
            .Where(t => t.IdentityUserId != null)
            .ToDictionary(t => t.IdentityUserId!, t => t);

        return links.Select(l =>
        {
            teacherByIdentity.TryGetValue(l.TrainerId, out var teacher);
            return new TrainerGymLinkResponseDto(
                l.Id,
                l.TrainerId,
                teacher?.Name ?? l.TrainerId,
                teacher?.Email ?? string.Empty,
                l.GymId,
                l.Status.ToString(),
                l.CreatedAt
            );
        }).ToList();
    }

    public async Task<bool> IsTrainerLinkedToGymAsync(string trainerId, int gymId)
    {
        var id = trainerId.Trim();
        return await _db.TrainerGymLinks.AnyAsync(l =>
            l.TrainerId == id &&
            l.GymId == gymId &&
            l.Status == TrainerGymLinkStatus.Approved);
    }

    private static GymResponseDto ToDto(Gym gym) => new(
        gym.Id,
        gym.Name,
        gym.CNPJ,
        gym.CommissionRate,
        gym.CancelFeeRate,
        gym.StripeAccountId,
        gym.IsActive,
        gym.OwnerUserId
    );

    private static GymLocationResponseDto ToDto(GymLocation location) => new(
        location.Id,
        location.GymId,
        location.Name,
        location.Address,
        location.City,
        location.State,
        location.ZipCode,
        location.Latitude,
        location.Longitude,
        location.IsActive
    );

    private static TrainerGymLinkResponseDto ToDto(TrainerGymLink link) => new(
        link.Id,
        link.TrainerId,
        TrainerName: link.TrainerId,
        TrainerEmail: string.Empty,
        link.GymId,
        link.Status.ToString(),
        link.CreatedAt
    );
}
