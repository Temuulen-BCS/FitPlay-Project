using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class RoomService : IRoomService
{
    private static readonly RoomBookingStatus[] BlockingStatuses =
    {
        RoomBookingStatus.Pending,
        RoomBookingStatus.Confirmed
    };

    private readonly FitPlayContext _db;

    public RoomService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<List<RoomResponseDto>> GetRoomsByLocationAsync(int locationId, bool? isActive = null)
    {
        var query = _db.Rooms.AsNoTracking().Where(r => r.GymLocationId == locationId).AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rooms = await query.OrderBy(r => r.Name).ToListAsync();
        return rooms.Select(ToDto).ToList();
    }

    public async Task<RoomResponseDto?> GetRoomByIdAsync(int roomId)
    {
        var room = await _db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId);
        return room is null ? null : ToDto(room);
    }

    public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomRequest request)
    {
        var room = new Room
        {
            GymLocationId = request.GymLocationId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Capacity = request.Capacity,
            PricePerHour = request.PricePerHour,
            IsActive = request.IsActive
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return ToDto(room);
    }

    public async Task<RoomResponseDto?> UpdateRoomAsync(int roomId, UpdateRoomRequest request)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room is null) return null;

        room.Name = request.Name.Trim();
        room.Description = request.Description?.Trim();
        room.Capacity = request.Capacity;
        room.PricePerHour = request.PricePerHour;
        room.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    public async Task<RoomAvailabilityResponseDto> GetRoomAvailabilityAsync(int roomId, DateOnly date)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        var bookings = await _db.RoomBookings.AsNoTracking()
            .Where(b => b.RoomId == roomId && b.StartTime < endOfDay && b.EndTime > startOfDay)
            .Where(b => BlockingStatuses.Contains(b.Status))
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        var slots = new List<RoomAvailabilitySlotDto>();
        var cursor = startOfDay;

        foreach (var booking in bookings)
        {
            if (booking.StartTime > cursor)
            {
                slots.Add(new RoomAvailabilitySlotDto(cursor, booking.StartTime, true, null));
            }

            var occupiedStart = booking.StartTime < startOfDay ? startOfDay : booking.StartTime;
            var occupiedEnd = booking.EndTime > endOfDay ? endOfDay : booking.EndTime;
            slots.Add(new RoomAvailabilitySlotDto(occupiedStart, occupiedEnd, false, booking.Id));
            cursor = occupiedEnd > cursor ? occupiedEnd : cursor;
        }

        if (cursor < endOfDay)
        {
            slots.Add(new RoomAvailabilitySlotDto(cursor, endOfDay, true, null));
        }

        return new RoomAvailabilityResponseDto(roomId, date, slots);
    }

    public async Task<List<RoomBookingResponseDto>> GetRoomBookingsAsync(int roomId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.RoomBookings.AsNoTracking().Where(b => b.RoomId == roomId).AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(b => b.EndTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(b => b.StartTime <= to.Value);
        }

        var bookings = await query.OrderBy(b => b.StartTime).ToListAsync();
        return bookings.Select(ToDto).ToList();
    }

    public async Task<RoomBookingResponseDto> CreateBookingAsync(int roomId, string trainerId, CreateRoomBookingRequest request)
    {
        var room = await _db.Rooms
            .Include(r => r.GymLocation)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null)
            throw new ArgumentException("Room not found.");

        if (room.GymLocation is null)
            throw new InvalidOperationException("Room location not found.");

        var normalizedTrainerId = trainerId.Trim();
        var isLinked = await _db.TrainerGymLinks.AnyAsync(l =>
            l.TrainerId == normalizedTrainerId &&
            l.GymId == room.GymLocation.GymId &&
            l.Status == TrainerGymLinkStatus.Approved);

        if (!isLinked)
            throw new InvalidOperationException("Trainer is not approved for this gym.");

        var purpose = ParsePurpose(request.Purpose);
        ValidateTimeRange(request.StartTime, request.EndTime);

        await EnsureNoConflictAsync(roomId, request.StartTime, request.EndTime);

        var booking = new RoomBooking
        {
            RoomId = roomId,
            TrainerId = normalizedTrainerId,
            Purpose = purpose,
            PurposeDescription = request.PurposeDescription?.Trim(),
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc),
            Status = RoomBookingStatus.Pending,
            TotalCost = CalculateCost(room.PricePerHour, request.StartTime, request.EndTime),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.RoomBookings.Add(booking);
        await _db.SaveChangesAsync();

        return ToDto(booking);
    }

    public async Task<RoomBookingResponseDto?> UpdateBookingAsync(int bookingId, string actorUserId, bool isAdmin, UpdateRoomBookingRequest request)
    {
        var booking = await _db.RoomBookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null) return null;

        var normalizedActor = actorUserId.Trim();
        if (!isAdmin && booking.TrainerId != normalizedActor)
            throw new UnauthorizedAccessException("You can only edit your own bookings.");

        ValidateTimeRange(request.StartTime, request.EndTime);
        var newStatus = ParseStatus(request.Status);

        if (BlockingStatuses.Contains(newStatus))
        {
            await EnsureNoConflictAsync(booking.RoomId, request.StartTime, request.EndTime, bookingId);
        }

        booking.Purpose = ParsePurpose(request.Purpose);
        booking.PurposeDescription = request.PurposeDescription?.Trim();
        booking.StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        booking.EndTime = DateTime.SpecifyKind(request.EndTime, DateTimeKind.Utc);
        booking.Status = newStatus;
        booking.Notes = request.Notes?.Trim();
        booking.TotalCost = CalculateCost(booking.Room?.PricePerHour ?? booking.TotalCost, request.StartTime, request.EndTime);
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(booking);
    }

    public async Task<bool> CancelBookingAsync(int bookingId, string actorUserId, bool isAdmin)
    {
        var booking = await _db.RoomBookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return false;

        var normalizedActor = actorUserId.Trim();
        if (!isAdmin && booking.TrainerId != normalizedActor)
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");

        booking.Status = RoomBookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<RoomResponseDto>> GetAvailableRoomsAsync(AvailableRoomsQueryDto query)
    {
        ValidateTimeRange(query.StartTime, query.EndTime);

        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.GymLocationId == query.GymLocationId && r.IsActive)
            .Where(r => !_db.RoomBookings.Any(b =>
                b.RoomId == r.Id &&
                BlockingStatuses.Contains(b.Status) &&
                b.StartTime < query.EndTime &&
                b.EndTime > query.StartTime))
            .OrderBy(r => r.Name)
            .ToListAsync();

        return rooms.Select(ToDto).ToList();
    }

    private async Task EnsureNoConflictAsync(int roomId, DateTime startTime, DateTime endTime, int? ignoreBookingId = null)
    {
        var hasConflict = await _db.RoomBookings.AnyAsync(b =>
            b.RoomId == roomId &&
            (ignoreBookingId == null || b.Id != ignoreBookingId.Value) &&
            BlockingStatuses.Contains(b.Status) &&
            b.StartTime < endTime &&
            b.EndTime > startTime);

        if (hasConflict)
            throw new InvalidOperationException("There is already a booking in this room for the selected interval.");
    }

    private static void ValidateTimeRange(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("EndTime must be greater than StartTime.");
    }

    private static decimal CalculateCost(decimal pricePerHour, DateTime startTime, DateTime endTime)
    {
        var hours = (decimal)(endTime - startTime).TotalMinutes / 60m;
        return decimal.Round(hours * pricePerHour, 2, MidpointRounding.AwayFromZero);
    }

    private static RoomBookingPurpose ParsePurpose(string purpose)
    {
        if (!Enum.TryParse<RoomBookingPurpose>(purpose, true, out var parsed))
            throw new ArgumentException("Invalid booking purpose.");
        return parsed;
    }

    private static RoomBookingStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<RoomBookingStatus>(status, true, out var parsed))
            throw new ArgumentException("Invalid booking status.");
        return parsed;
    }

    private static RoomResponseDto ToDto(Room room) => new(
        room.Id,
        room.GymLocationId,
        room.Name,
        room.Description,
        room.Capacity,
        room.PricePerHour,
        room.IsActive
    );

    private static RoomBookingResponseDto ToDto(RoomBooking booking) => new(
        booking.Id,
        booking.RoomId,
        booking.TrainerId,
        booking.Purpose.ToString(),
        booking.PurposeDescription,
        booking.StartTime,
        booking.EndTime,
        booking.Status.ToString(),
        booking.TotalCost,
        booking.Notes,
        booking.CreatedAt,
        booking.UpdatedAt
    );
}
