using FitPlay.Domain.Data;
using FitPlay.Domain.DTOs;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Domain.Services;

public class ClassScheduleService
{
    private readonly FitPlayContext _db;

    public ClassScheduleService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<List<ClassScheduleDto>> GetUserScheduleAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(s => s.ScheduledAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.ScheduledAt <= to.Value);
        }

        var items = await query
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    public async Task<ClassScheduleDto?> GetByIdAsync(int id)
    {
        var schedule = await _db.ClassSchedules
            .FirstOrDefaultAsync(s => s.Id == id);

        return schedule == null ? null : ToDto(schedule);
    }

    public async Task<ClassScheduleDto> CreateAsync(CreateClassScheduleRequest request)
    {
        var schedule = new ClassSchedule
        {
            UserId = request.UserId,
            Modality = request.Modality.Trim(),
            ScheduledAt = request.ScheduledAt,
            Notes = request.Notes,
            Status = ClassScheduleStatus.Scheduled
        };

        _db.ClassSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(schedule.Id) ?? ToDto(schedule);
    }

    public async Task<ClassScheduleDto?> UpdateAsync(int id, UpdateClassScheduleRequest request)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.Modality = request.Modality.Trim();
        schedule.ScheduledAt = request.ScheduledAt;
        schedule.Notes = request.Notes;
        schedule.Status = ParseStatus(request.Status);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return false;

        _db.ClassSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ClassScheduleWithTrainerDto>> GetPublicSchedulesAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.ClassSchedules
            .Where(s => s.Status == ClassScheduleStatus.Scheduled && s.ScheduledAt > DateTime.UtcNow)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.ScheduledAt >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.ScheduledAt <= to.Value);

        var items = await query
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();

        return items.Select(s => new ClassScheduleWithTrainerDto(
            s.Id,
            s.UserId,
            s.Modality,
            s.ScheduledAt,
            s.Status.ToString(),
            s.Notes
        )).ToList();
    }

    public async Task<ClassScheduleDto?> BookClassAsync(int scheduleId, int userId)
    {
        var schedule = await _db.ClassSchedules.FindAsync(scheduleId);
        if (schedule == null) return null;

        if (schedule.UserId == userId)
            return null;

        schedule.Status = ClassScheduleStatus.Scheduled;
        await _db.SaveChangesAsync();

        return ToDto(schedule);
    }

    public async Task<ClassScheduleDto?> UpdateStatusAsync(int id, string status)
    {
        var schedule = await _db.ClassSchedules.FindAsync(id);
        if (schedule == null) return null;

        schedule.Status = ParseStatus(status);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private static ClassScheduleDto ToDto(ClassSchedule schedule)
    {
        return new ClassScheduleDto(
            Id: schedule.Id,
            UserId: schedule.UserId,
            Modality: schedule.Modality,
            ScheduledAt: schedule.ScheduledAt,
            Status: schedule.Status.ToString(),
            Notes: schedule.Notes
        );
    }

    private static ClassScheduleStatus ParseStatus(string status)
    {
        return Enum.TryParse<ClassScheduleStatus>(status, true, out var parsed)
            ? parsed
            : ClassScheduleStatus.Scheduled;
    }
}
