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
    private readonly IClockService _clock;

    public GymVisitService(FitPlayContext db, IClockService clock)
    {
        _db = db;
        _clock = clock;
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

        // Check if user has a confirmed (paid) class booking at this gym location
        // Allow check-in up to 5 minutes before class starts, or while class is in progress
        // Check both ClassEnrollment system AND ClassSchedule system
        // Also accept Completed status — auto-completer may mark a class Completed mid-session.
        var now = _clock.UtcNow;

        // --- Check 1: ClassEnrollment system ---
        var hasConfirmedEnrollment = await (
            from e in _db.ClassEnrollments
            join s in _db.ClassSessions on e.ClassSessionId equals s.Id
            join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where e.UserId == normalizedUserId
                && (e.Status == ClassEnrollmentStatus.Confirmed || e.Status == ClassEnrollmentStatus.Completed)
                && s.StartTime <= now.AddMinutes(5)
                && s.EndTime >= now
                && r.GymLocationId == request.GymLocationId
            select e.Id
        ).AnyAsync();

        // --- Check 2: ClassSchedule system (book-class page bookings) ---
        if (!hasConfirmedEnrollment)
        {
            hasConfirmedEnrollment = await (
                from cs in _db.ClassSchedules
                join u in _db.Users on cs.UserId equals u.Id
                join rb in _db.RoomBookings on cs.RoomBookingId equals rb.Id
                join r in _db.Rooms on rb.RoomId equals r.Id
                where u.IdentityUserId == normalizedUserId
                    && (cs.Status == ClassScheduleStatus.Scheduled || cs.Status == ClassScheduleStatus.Completed)
                    && (cs.PaymentStatus == ClassSchedulePaymentStatus.Completed
                        || cs.PaymentStatus == ClassSchedulePaymentStatus.None)
                    && rb.StartTime <= now.AddMinutes(5)
                    && rb.EndTime >= now
                    && r.GymLocationId == request.GymLocationId
                select cs.Id
            ).AnyAsync();
        }

        if (!hasConfirmedEnrollment)
            throw new InvalidOperationException("NO_CONFIRMED_ENROLLMENT: You must have a confirmed (paid) class booking at this gym during the current time to check in.");

        // Create the visit
        var visit = new GymVisit
        {
            UserId = normalizedUserId,
            GymLocationId = request.GymLocationId,
            CheckInTime = _clock.UtcNow,
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
        activeVisit.CheckOutTime = _clock.UtcNow;
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

    public async Task<CheckInEligibilityDto> GetCheckInEligibilityAsync(string userId, int gymLocationId)
    {
        var normalizedUserId = userId.Trim();
        var now = _clock.UtcNow;

        // --- Check 1: ClassEnrollment system (session-based enrollments) ---
        // Accept Confirmed OR Completed status — auto-completer may have marked a class Completed
        // mid-session if it ran while the class was in progress.
        var nextEnrollment = await (
            from e in _db.ClassEnrollments
            join s in _db.ClassSessions on e.ClassSessionId equals s.Id
            join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where e.UserId == normalizedUserId
                && (e.Status == ClassEnrollmentStatus.Confirmed || e.Status == ClassEnrollmentStatus.Completed)
                && s.EndTime >= now
                && r.GymLocationId == gymLocationId
            orderby s.StartTime
            select new { s.StartTime, s.EndTime, s.Title }
        ).FirstOrDefaultAsync();

        // --- Check 2: ClassSchedule system (book-class page / my-sessions bookings) ---
        // Accept Scheduled OR Completed status — auto-completer marks a ClassSchedule as Completed
        // once its RoomBooking.EndTime <= now, but with mock clock the end time may still be in the
        // future from the user's perspective even though it was already auto-completed in real time.
        var nextSchedule = await (
            from cs in _db.ClassSchedules
            join u in _db.Users on cs.UserId equals u.Id
            join rb in _db.RoomBookings on cs.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where u.IdentityUserId == normalizedUserId
                && (cs.Status == ClassScheduleStatus.Scheduled || cs.Status == ClassScheduleStatus.Completed)
                && (cs.PaymentStatus == ClassSchedulePaymentStatus.Completed
                    || cs.PaymentStatus == ClassSchedulePaymentStatus.None)
                && rb.EndTime >= now
                && r.GymLocationId == gymLocationId
            orderby rb.StartTime
            select new { StartTime = rb.StartTime, EndTime = rb.EndTime, Title = cs.Modality }
        ).FirstOrDefaultAsync();

        // --- Check 3a: Past ClassEnrollment ---
        var pastEnrollmentTitle = await (
            from e in _db.ClassEnrollments
            join s in _db.ClassSessions on e.ClassSessionId equals s.Id
            join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where e.UserId == normalizedUserId
                && e.Status != ClassEnrollmentStatus.Cancelled
                && s.EndTime < now
                && r.GymLocationId == gymLocationId
            orderby s.EndTime descending
            select s.Title
        ).FirstOrDefaultAsync();

        // --- Check 3b: Past ClassSchedule (only if 3a found nothing) ---
        string? pastClassTitle = pastEnrollmentTitle;
        if (pastClassTitle == null)
        {
            pastClassTitle = await (
                from cs in _db.ClassSchedules
                join u in _db.Users on cs.UserId equals u.Id
                join rb in _db.RoomBookings on cs.RoomBookingId equals rb.Id
                join r in _db.Rooms on rb.RoomId equals r.Id
                where u.IdentityUserId == normalizedUserId
                    && cs.Status != ClassScheduleStatus.Cancelled
                    && (cs.PaymentStatus == ClassSchedulePaymentStatus.Completed
                        || cs.PaymentStatus == ClassSchedulePaymentStatus.None)
                    && rb.EndTime < now
                    && r.GymLocationId == gymLocationId
                orderby rb.EndTime descending
                select cs.Modality
            ).FirstOrDefaultAsync();
        }

        var hasEnrollment = nextEnrollment != null || nextSchedule != null;
        var hasPastClass  = pastClassTitle != null;

        if (hasEnrollment)
        {
            // Pick the earlier of the two upcoming classes
            DateTime startTime, endTime;
            string? title;

            if (nextEnrollment != null && (nextSchedule == null || nextEnrollment.StartTime <= nextSchedule.StartTime))
            {
                startTime = nextEnrollment.StartTime;
                endTime   = nextEnrollment.EndTime;
                title     = nextEnrollment.Title;
            }
            else
            {
                startTime = nextSchedule!.StartTime;
                endTime   = nextSchedule.EndTime;
                title     = nextSchedule.Title;
            }

            var canCheckInNow = startTime <= now.AddMinutes(5) && endTime >= now;
            return new CheckInEligibilityDto(
                HasEnrollment: true,
                CanCheckInNow: canCheckInNow,
                NextClassStartTime: startTime,
                NextClassEndTime: endTime,
                NextClassTitle: title,
                HasPastClass: hasPastClass,
                PastClassTitle: pastClassTitle);
        }

        if (hasPastClass)
        {
            return new CheckInEligibilityDto(
                HasEnrollment: false,
                CanCheckInNow: false,
                NextClassStartTime: null,
                NextClassEndTime: null,
                NextClassTitle: null,
                HasPastClass: true,
                PastClassTitle: pastClassTitle);
        }

        return new CheckInEligibilityDto(false, false, null, null, null);
    }

    public async Task<List<PastClassDto>> GetPastClassesAsync(string userId, int gymLocationId)
    {
        var normalizedUserId = userId.Trim();
        var now = _clock.UtcNow;

        var pastEnrollments = await (
            from e in _db.ClassEnrollments
            join s in _db.ClassSessions on e.ClassSessionId equals s.Id
            join rb in _db.RoomBookings on s.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where e.UserId == normalizedUserId
                && e.Status != ClassEnrollmentStatus.Cancelled
                && s.EndTime < now
                && r.GymLocationId == gymLocationId
            select new
            {
                Title = s.Title,
                ClassStartTime = s.StartTime,
                ClassEndTime = s.EndTime,
                BookingSource = "Session Enrollment",
                RoomName = r.Name
            }
        ).ToListAsync();

        var pastSchedules = await (
            from cs in _db.ClassSchedules
            join u in _db.Users on cs.UserId equals u.Id
            join rb in _db.RoomBookings on cs.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where u.IdentityUserId == normalizedUserId
                && cs.Status != ClassScheduleStatus.Cancelled
                && (cs.PaymentStatus == ClassSchedulePaymentStatus.Completed
                    || cs.PaymentStatus == ClassSchedulePaymentStatus.None)
                && rb.EndTime < now
                && r.GymLocationId == gymLocationId
            select new
            {
                Title = cs.Modality,
                ClassStartTime = rb.StartTime,
                ClassEndTime = rb.EndTime,
                BookingSource = "Direct Booking",
                RoomName = r.Name
            }
        ).ToListAsync();

        var allClasses = pastEnrollments
            .Concat(pastSchedules)
            .GroupBy(c => new { c.Title, c.ClassStartTime, c.ClassEndTime, c.BookingSource, c.RoomName })
            .Select(g => g.Key)
            .OrderByDescending(c => c.ClassStartTime)
            .ToList();

        if (!allClasses.Any())
            return new List<PastClassDto>();

        var visits = await _db.GymVisits
            .AsNoTracking()
            .Where(v =>
                v.UserId == normalizedUserId
                && v.GymLocationId == gymLocationId
                && v.CheckOutTime != null)
            .Select(v => new { v.CheckInTime, v.CheckOutTime })
            .ToListAsync();

        return allClasses
            .Select(c =>
            {
                var matchedVisit = visits
                    .Where(v =>
                        v.CheckInTime >= c.ClassStartTime.AddMinutes(-10)
                        && v.CheckInTime <= c.ClassEndTime
                        && v.CheckOutTime!.Value >= c.ClassStartTime)
                    .OrderBy(v => v.CheckInTime)
                    .FirstOrDefault();

                int? durationMinutes = matchedVisit != null
                    ? (int)Math.Max(0, (matchedVisit.CheckOutTime!.Value - matchedVisit.CheckInTime).TotalMinutes)
                    : null;

                return new PastClassDto(
                    c.Title,
                    c.ClassStartTime,
                    c.ClassEndTime,
                    matchedVisit?.CheckInTime,
                    matchedVisit?.CheckOutTime,
                    durationMinutes,
                    c.BookingSource,
                    c.RoomName);
            })
            .OrderByDescending(x => x.ClassStartTime)
            .ToList();
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
        // ── 1. Load active visits at this location ──
        var activeVisits = await _db.GymVisits
            .AsNoTracking()
            .Where(v => v.GymLocationId == gymLocationId && v.CheckOutTime == null)
            .ToListAsync();

        if (!activeVisits.Any())
            return new List<ActiveVisitDetailDto>();

        var visitUserIds = activeVisits.Select(v => v.UserId).Distinct().ToList();

        // ── 2. Batch-load domain Users (Identity ID → User) ──
        var usersDict = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var usersList = await _db.Users
            .AsNoTracking()
            .Where(u => u.IdentityUserId != null && visitUserIds.Contains(u.IdentityUserId))
            .ToListAsync();
        foreach (var u in usersList)
            if (u.IdentityUserId != null)
                usersDict[u.IdentityUserId] = u;

        // ── 3. Batch-load ALL teachers (for name resolution later) ──
        var allTeachers = await _db.Teachers.AsNoTracking().ToListAsync();
        var teachersByIdentityId = new Dictionary<string, Teacher>(StringComparer.OrdinalIgnoreCase);
        var teachersById = new Dictionary<int, Teacher>();
        foreach (var t in allTeachers)
        {
            teachersById[t.Id] = t;
            if (!string.IsNullOrEmpty(t.IdentityUserId))
                teachersByIdentityId[t.IdentityUserId] = t;
        }

        // ── 4. Batch-load TrainerGymLinks for this gym (name fallback) ──
        var gymId = await _db.GymLocations
            .Where(gl => gl.Id == gymLocationId)
            .Select(gl => gl.GymId)
            .FirstOrDefaultAsync();
        var trainerLinkNames = new Dictionary<string, Teacher>(StringComparer.OrdinalIgnoreCase);
        if (gymId > 0)
        {
            var links = await _db.TrainerGymLinks
                .AsNoTracking()
                .Where(tl => tl.GymId == gymId && tl.Status == TrainerGymLinkStatus.Approved)
                .ToListAsync();
            foreach (var link in links)
            {
                if (!string.IsNullOrEmpty(link.TrainerId) && teachersByIdentityId.TryGetValue(link.TrainerId, out var teacher))
                    trainerLinkNames[link.TrainerId] = teacher;
            }
        }

        // ── 5. Batch-load today's data (avoid N+1 per-visit queries) ──
        var today = _clock.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // 5a. Today's room bookings at this location
        var todayBookings = await (
            from rb in _db.RoomBookings
            join r in _db.Rooms on rb.RoomId equals r.Id
            where r.GymLocationId == gymLocationId
               && rb.Status != RoomBookingStatus.Cancelled
               && rb.StartTime < tomorrow
               && rb.EndTime >= today
            select new
            {
                BookingId = rb.Id,
                rb.RoomId,
                rb.TrainerId,
                rb.StartTime,
                rb.EndTime,
                rb.Modality,
                rb.PaidAmount,
                RoomName = r.Name
            }
        ).AsNoTracking().ToListAsync();

        // 5b. Today's class enrollments for our checked-in users at this location
        var todayEnrollments = await (
            from ce in _db.ClassEnrollments
            join cs in _db.ClassSessions on ce.ClassSessionId equals cs.Id
            join rb in _db.RoomBookings on cs.RoomBookingId equals rb.Id
            join r in _db.Rooms on rb.RoomId equals r.Id
            where visitUserIds.Contains(ce.UserId)
               && r.GymLocationId == gymLocationId
               && (ce.Status == ClassEnrollmentStatus.Confirmed
                   || ce.Status == ClassEnrollmentStatus.Completed)
               && cs.Status != ClassSessionStatus.Cancelled
               && cs.StartTime < tomorrow
               && cs.EndTime >= today
            select new
            {
                ce.UserId,
                ClassSessionId = cs.Id,
                ClassTitle = cs.Title,
                SessionStartTime = cs.StartTime,
                SessionEndTime = cs.EndTime,
                SessionTrainerId = cs.TrainerId,   // string (Identity ID)
                RoomName = r.Name,
                ce.PaidAmount,
                EnrollmentStatus = ce.Status.ToString()
            }
        ).AsNoTracking().ToListAsync();

        // 5c. Today's class schedules for our checked-in users
        var domainUserIds = usersDict.Values.Select(u => u.Id).Distinct().ToList();
        var todaySchedules = domainUserIds.Any()
            ? await _db.ClassSchedules
                .AsNoTracking()
                .Where(cs => cs.UserId.HasValue
                    && domainUserIds.Contains(cs.UserId.Value)
                    && cs.Status != ClassScheduleStatus.Cancelled
                    && cs.ScheduledAt >= today && cs.ScheduledAt < tomorrow)
                .ToListAsync()
            : new List<ClassSchedule>();

        // ── 6. Resolve per visit ──
        var result = new List<ActiveVisitDetailDto>();

        foreach (var visit in activeVisits)
        {
            var userInfo = usersDict.TryGetValue(visit.UserId, out var ui) ? ui : null;
            var checkInTime = visit.CheckInTime;

            string trainerName = string.Empty;
            string trainerEmail = string.Empty;
            string? resolvedTrainerId = null;
            int? dtoClassSessionId = null;
            string? dtoClassTitle = null;
            DateTime? dtoSessionStart = null;
            DateTime? dtoSessionEnd = null;
            string? dtoRoomName = null;
            decimal? dtoPaidAmount = null;
            string? dtoEnrollmentStatus = null;

            // --- Path 1: ClassEnrollment system ---
            // Match this user's enrollments today at this location; pick closest to check-in time
            var enrollment = todayEnrollments
                .Where(e => string.Equals(e.UserId, visit.UserId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => Math.Abs((e.SessionStartTime - checkInTime).TotalMinutes))
                .FirstOrDefault();

            if (enrollment != null)
            {
                dtoClassSessionId = enrollment.ClassSessionId;
                dtoClassTitle = enrollment.ClassTitle;
                dtoSessionStart = enrollment.SessionStartTime;
                dtoSessionEnd = enrollment.SessionEndTime;
                dtoRoomName = enrollment.RoomName;
                dtoPaidAmount = enrollment.PaidAmount;
                dtoEnrollmentStatus = enrollment.EnrollmentStatus;
                resolvedTrainerId = enrollment.SessionTrainerId;

                if (!string.IsNullOrEmpty(enrollment.SessionTrainerId)
                    && teachersByIdentityId.TryGetValue(enrollment.SessionTrainerId, out var t1))
                {
                    trainerName = t1.Name;
                    trainerEmail = t1.Email;
                }
            }

            // --- Path 2: ClassSchedule system ---
            // ClassSchedule.RoomBookingId is nullable, so we do NOT inner-join; we look it up separately.
            if (enrollment == null && userInfo != null)
            {
                var schedule = todaySchedules
                    .Where(s => s.UserId == userInfo.Id)
                    .OrderBy(s => Math.Abs((s.ScheduledAt - checkInTime).TotalMinutes))
                    .FirstOrDefault();

                if (schedule != null)
                {
                    dtoClassTitle = schedule.Modality;
                    dtoEnrollmentStatus = schedule.Status.ToString();
                    dtoPaidAmount = schedule.PaidAmount;

                    // Optional: resolve room/time from linked RoomBooking (if set)
                    if (schedule.RoomBookingId.HasValue)
                    {
                        var linkedBooking = todayBookings.FirstOrDefault(b => b.BookingId == schedule.RoomBookingId.Value);
                        if (linkedBooking != null)
                        {
                            dtoSessionStart = linkedBooking.StartTime;
                            dtoSessionEnd = linkedBooking.EndTime;
                            dtoRoomName = linkedBooking.RoomName;
                        }
                    }

                    // Resolve trainer: ClassSchedule.TrainerId is int? (domain Teacher.Id)
                    if (schedule.TrainerId.HasValue && teachersById.TryGetValue(schedule.TrainerId.Value, out var t2))
                    {
                        resolvedTrainerId = t2.IdentityUserId ?? t2.Id.ToString();
                        trainerName = t2.Name;
                        trainerEmail = t2.Email;
                    }
                }
            }

            // --- Path 3: RoomBooking fallback ---
            // Find the closest booking today at this location (by start time to check-in).
            // Fills in any missing fields (class title, room, trainer).
            if (string.IsNullOrEmpty(resolvedTrainerId) || string.IsNullOrEmpty(dtoClassTitle))
            {
                var closestBooking = todayBookings
                    .Where(b => !string.IsNullOrEmpty(b.TrainerId))
                    .OrderBy(b => Math.Abs((b.StartTime - checkInTime).TotalMinutes))
                    .FirstOrDefault();

                if (closestBooking != null)
                {
                    resolvedTrainerId ??= closestBooking.TrainerId;
                    dtoClassTitle ??= closestBooking.Modality;
                    dtoSessionStart ??= closestBooking.StartTime;
                    dtoSessionEnd ??= closestBooking.EndTime;
                    dtoRoomName ??= closestBooking.RoomName;
                    dtoPaidAmount ??= closestBooking.PaidAmount;

                    if (string.IsNullOrEmpty(trainerName)
                        && !string.IsNullOrEmpty(closestBooking.TrainerId)
                        && teachersByIdentityId.TryGetValue(closestBooking.TrainerId, out var t3))
                    {
                        trainerName = t3.Name;
                        trainerEmail = t3.Email;
                    }
                }
            }

            // --- Path 4: Broadest fallback (any recent booking at this location) ---
            // If today's data had no matches, find the most recent non-cancelled booking
            // at this location so the card always shows class/trainer info.
            if (string.IsNullOrEmpty(resolvedTrainerId) && string.IsNullOrEmpty(dtoClassTitle))
            {
                var recentBooking = await (
                    from rb in _db.RoomBookings
                    join r in _db.Rooms on rb.RoomId equals r.Id
                    where r.GymLocationId == gymLocationId
                       && !string.IsNullOrEmpty(rb.TrainerId)
                       && rb.Status != RoomBookingStatus.Cancelled
                    orderby rb.StartTime descending
                    select new
                    {
                        rb.TrainerId,
                        rb.StartTime,
                        rb.EndTime,
                        rb.Modality,
                        rb.PaidAmount,
                        RoomName = r.Name
                    }
                ).AsNoTracking().FirstOrDefaultAsync();

                if (recentBooking != null)
                {
                    resolvedTrainerId ??= recentBooking.TrainerId;
                    dtoClassTitle ??= recentBooking.Modality;
                    dtoSessionStart ??= recentBooking.StartTime;
                    dtoSessionEnd ??= recentBooking.EndTime;
                    dtoRoomName ??= recentBooking.RoomName;
                    dtoPaidAmount ??= recentBooking.PaidAmount;

                    if (string.IsNullOrEmpty(trainerName)
                        && !string.IsNullOrEmpty(recentBooking.TrainerId)
                        && teachersByIdentityId.TryGetValue(recentBooking.TrainerId, out var t4))
                    {
                        trainerName = t4.Name;
                        trainerEmail = t4.Email;
                    }
                }
            }

            // --- Trainer name fallback: TrainerGymLinks lookup ---
            // If we have a TrainerId but no name yet, try the pre-loaded gym link teachers
            if (!string.IsNullOrEmpty(resolvedTrainerId) && string.IsNullOrEmpty(trainerName))
            {
                if (trainerLinkNames.TryGetValue(resolvedTrainerId, out var linkTeacher))
                {
                    trainerName = linkTeacher.Name;
                    trainerEmail = linkTeacher.Email;
                }
            }

            result.Add(new ActiveVisitDetailDto(
                visit.Id,
                visit.UserId,
                userInfo?.Name ?? string.Empty,
                userInfo?.Email ?? string.Empty,
                userInfo?.Phone,
                visit.CheckInTime,
                dtoClassSessionId,
                dtoClassTitle,
                dtoSessionStart,
                dtoSessionEnd,
                resolvedTrainerId,
                trainerName,
                trainerEmail,
                dtoRoomName,
                dtoPaidAmount,
                dtoEnrollmentStatus
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
