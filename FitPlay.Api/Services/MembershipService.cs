using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Services;

public class MembershipService
{
    private readonly FitPlayContext _db;

    public MembershipService(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(int clientId)
    {
        return await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.ClientId == clientId && s.Status == "Active")
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription?> GetLatestSubscriptionAsync(int clientId)
    {
        return await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.ClientId == clientId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription> UpsertSubscriptionAsync(
        int clientId,
        string status,
        DateTime startDate,
        DateTime? endDate,
        string? stripeCustomerId,
        string? stripeSubscriptionId,
        int teacherId = 0)
    {
        var existing = await _db.Subscriptions
            .Where(s => s.ClientId == clientId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            var created = new Subscription
            {
                ClientId = clientId,
                TeacherId = teacherId,
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                StripeCustomerId = stripeCustomerId,
                StripeSubscriptionId = stripeSubscriptionId
            };
            _db.Subscriptions.Add(created);
            await _db.SaveChangesAsync();
            return created;
        }

        existing.Status = status;
        existing.StartDate = startDate;
        existing.EndDate = endDate;
        existing.StripeCustomerId = stripeCustomerId ?? existing.StripeCustomerId;
        existing.StripeSubscriptionId = stripeSubscriptionId ?? existing.StripeSubscriptionId;

        await _db.SaveChangesAsync();
        return existing;
    }
}
