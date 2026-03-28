using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class GymVisitService : IGymVisitService
{
    private const double CheckInMaxDistanceMeters = 200.0;
    private const double CheckOutMinDistanceMeters = 200.0;

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

    public async Task<List<LocationPresenceDto>> GetActiveCountsByGymAsync(int gymId)
    {
        var locationCounts = await _db.GymVisits
            .Include(v => v.GymLocation)
            .Where(v => v.CheckOutTime == null && v.GymLocation.GymId == gymId && v.GymLocation.IsActive)
            .GroupBy(v => new { v.GymLocationId, v.GymLocation.Name })
            .Select(g => new LocationPresenceDto(
                g.Key.GymLocationId,
                g.Key.Name,
                g.Count()))
            .ToListAsync();

        // Also include locations with zero active visits
        var allLocationsInGym = await _db.GymLocations
            .AsNoTracking()
            .Where(gl => gl.GymId == gymId && gl.IsActive)
            .Select(gl => new { gl.Id, gl.Name })
            .ToListAsync();

        var locationCountsDict = locationCounts.ToDictionary(lc => lc.GymLocationId);

        var result = allLocationsInGym.Select(loc => 
            locationCountsDict.ContainsKey(loc.Id) 
                ? locationCountsDict[loc.Id]
                : new LocationPresenceDto(loc.Id, loc.Name, 0)
        ).OrderBy(lc => lc.LocationName).ToList();

        return result;
    }

    public async Task<int?> GetGymIdFromLocationIdAsync(int gymLocationId)
    {
        var gymLocation = await _db.GymLocations
            .AsNoTracking()
            .Where(gl => gl.Id == gymLocationId)
            .Select(gl => gl.GymId)
            .FirstOrDefaultAsync();

        return gymLocation == 0 ? null : gymLocation;
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

    public async Task<List<ActiveVisitDetailDto>> GetActiveVisitDetailsByLocationAsync(int gymLocationId)
    {
        var query = from visit in _db.GymVisits
                   join gymLocation in _db.GymLocations on visit.GymLocationId equals gymLocation.Id
                   where visit.GymLocationId == gymLocationId && visit.CheckOutTime == null
                   select new
                   {
                       visit.Id,
                       visit.UserId,
                       visit.CheckInTime,
                       gymLocation.Name
                   };

        var activeVisits = await query.ToListAsync();
        var result = new List<ActiveVisitDetailDto>();

        foreach (var visit in activeVisits)
        {
            // Find user details from Identity system (we'll need to pass this through the API layer)
            // For now, we'll work with what we have and let the controller layer handle user lookups

            // Look for any active class sessions for this user at this location
            var classDetails = await (from enrollment in _db.ClassEnrollments
                                    join classSession in _db.ClassSessions on enrollment.ClassSessionId equals classSession.Id
                                    join roomBooking in _db.RoomBookings on classSession.RoomBookingId equals roomBooking.Id
                                    join room in _db.Rooms on roomBooking.RoomId equals room.Id
                                    where enrollment.UserId == visit.UserId
                                       && room.GymLocationId == gymLocationId
                                       && enrollment.Status == ClassEnrollmentStatus.Confirmed
                                       && classSession.Status == ClassSessionStatus.Ongoing
                                       && DateTime.UtcNow >= classSession.StartTime
                                       && DateTime.UtcNow <= classSession.EndTime
                                    select new
                                    {
                                        ClassSessionId = classSession.Id,
                                        ClassTitle = classSession.Title,
                                        SessionStartTime = classSession.StartTime,
                                        SessionEndTime = classSession.EndTime,
                                        TrainerId = classSession.TrainerId,
                                        RoomName = room.Name,
                                        PaidAmount = enrollment.PaidAmount,
                                        EnrollmentStatus = enrollment.Status.ToString()
                                    }).FirstOrDefaultAsync();

            result.Add(new ActiveVisitDetailDto(
                visit.Id,
                visit.UserId,
                string.Empty, // UserName - will be populated in controller
                string.Empty, // UserEmail - will be populated in controller
                null, // UserPhone - will be populated in controller
                visit.CheckInTime,
                classDetails?.ClassSessionId,
                classDetails?.ClassTitle,
                classDetails?.SessionStartTime,
                classDetails?.SessionEndTime,
                classDetails?.TrainerId,
                string.Empty, // TrainerName - will be populated in controller
                string.Empty, // TrainerEmail - will be populated in controller
                classDetails?.RoomName,
                classDetails?.PaidAmount,
                classDetails?.EnrollmentStatus
            ));
        }

        return result;
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