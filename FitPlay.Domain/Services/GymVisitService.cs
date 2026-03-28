using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class GymVisitService : IGymVisitService
{
    private const double CheckInMaxDistanceMeters = 200.0;
    private const double CheckOutMinDistanceMeters = 500.0;

    private readonly FitPlayContext _db;

    public GymVisitService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<GymVisitResponseDto> CheckInAsync(string userId, GymCheckInRequest request)
    {
        var normalizedUserId = userId.Trim();

        // Check if user already has an active visit
        var activeVisit = await _db.GymVisits
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == normalizedUserId && v.CheckOutTime == null);

        if (activeVisit != null)
            throw new InvalidOperationException("You already have an active gym visit. Check out first before checking in to another gym.");

        // Get the gym location
        var gymLocation = await _db.GymLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(gl => gl.Id == request.GymLocationId && gl.IsActive);

        if (gymLocation == null)
            throw new ArgumentException("Gym location not found or inactive.");

        if (!gymLocation.Latitude.HasValue || !gymLocation.Longitude.HasValue)
            throw new InvalidOperationException("Gym location does not have coordinates set.");

        // Calculate distance using Haversine formula
        var distance = CalculateDistanceInMeters(
            request.Latitude, request.Longitude,
            gymLocation.Latitude.Value, gymLocation.Longitude.Value);

        if (distance > CheckInMaxDistanceMeters)
            throw new InvalidOperationException($"You are too far from the gym. You must be within {CheckInMaxDistanceMeters}m to check in. Current distance: {distance:F0}m");

        // Create the visit
        var visit = new GymVisit
        {
            UserId = normalizedUserId,
            GymLocationId = request.GymLocationId,
            CheckInTime = DateTime.UtcNow,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude
        };

        _db.GymVisits.Add(visit);
        await _db.SaveChangesAsync();

        return ToDto(visit, gymLocation.Name);
    }

    public async Task<GymVisitResponseDto> CheckOutAsync(string userId, GymCheckOutRequest request)
    {
        var normalizedUserId = userId.Trim();

        // Find the active visit
        var activeVisit = await _db.GymVisits
            .Include(v => v.GymLocation)
            .FirstOrDefaultAsync(v => v.UserId == normalizedUserId && v.CheckOutTime == null);

        if (activeVisit == null)
            throw new InvalidOperationException("No active gym visit found.");

        if (activeVisit.GymLocation?.Latitude == null || activeVisit.GymLocation?.Longitude == null)
            throw new InvalidOperationException("Gym location coordinates are not available.");

        // Calculate distance from the gym where check-in was made
        var distance = CalculateDistanceInMeters(
            request.Latitude, request.Longitude,
            activeVisit.GymLocation.Latitude.Value, activeVisit.GymLocation.Longitude.Value);

        if (distance < CheckOutMinDistanceMeters)
            throw new InvalidOperationException($"You are too close to the gym. You must be at least {CheckOutMinDistanceMeters}m away to check out. Current distance: {distance:F0}m");

        // Update the visit with checkout info
        activeVisit.CheckOutTime = DateTime.UtcNow;
        activeVisit.CheckOutLatitude = request.Latitude;
        activeVisit.CheckOutLongitude = request.Longitude;

        await _db.SaveChangesAsync();

        return ToDto(activeVisit, activeVisit.GymLocation.Name);
    }

    public async Task<GymVisitResponseDto?> GetActiveVisitAsync(string userId)
    {
        var normalizedUserId = userId.Trim();

        var activeVisit = await _db.GymVisits
            .Include(v => v.GymLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == normalizedUserId && v.CheckOutTime == null);

        return activeVisit != null ? ToDto(activeVisit, activeVisit.GymLocation?.Name ?? "Unknown") : null;
    }

    public async Task<List<GymVisitResponseDto>> GetVisitHistoryAsync(string userId, int limit = 50)
    {
        var normalizedUserId = userId.Trim();

        var visits = await _db.GymVisits
            .Include(v => v.GymLocation)
            .AsNoTracking()
            .Where(v => v.UserId == normalizedUserId)
            .OrderByDescending(v => v.CheckInTime)
            .Take(limit)
            .ToListAsync();

        return visits.Select(v => ToDto(v, v.GymLocation?.Name ?? "Unknown")).ToList();
    }

    public async Task<List<GymLocationForCheckInDto>> GetAllActiveGymLocationsAsync()
    {
        var locations = await _db.GymLocations
            .AsNoTracking()
            .Where(gl => gl.IsActive && gl.Latitude.HasValue && gl.Longitude.HasValue)
            .OrderBy(gl => gl.Name)
            .ToListAsync();

        return locations.Select(gl => new GymLocationForCheckInDto(
            gl.Id,
            gl.Name,
            gl.Address,
            gl.City,
            gl.State,
            gl.Latitude,
            gl.Longitude)).ToList();
    }

    /// <summary>
    /// Calculates the great circle distance between two points on Earth using the Haversine formula.
    /// Returns distance in meters.
    /// </summary>
    private static double CalculateDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var distanceKm = EarthRadiusKm * c;
        return distanceKm * 1000; // Convert to meters
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    private static GymVisitResponseDto ToDto(GymVisit visit, string gymLocationName) => new(
        visit.Id,
        visit.UserId,
        visit.GymLocationId,
        gymLocationName,
        visit.CheckInTime,
        visit.CheckOutTime,
        visit.CheckInLatitude,
        visit.CheckInLongitude,
        visit.CheckOutLatitude,
        visit.CheckOutLongitude
    );
}