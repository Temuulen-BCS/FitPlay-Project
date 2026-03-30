using FitPlay.Domain.Data;
using FitPlay.Domain.Models;
using FitPlay.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FitPlay.Api.Services;

public class MembershipService
{
    private readonly FitPlayContext _db;
    private readonly IClockService _clock;

    public MembershipService(FitPlayContext db, IClockService clock)
    {
        _db = db;
        _clock = clock;
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

    // ✅ Chamado pelo webhook invoice.payment_succeeded e customer.subscription.updated (active)
    public async Task ActivateSubscriptionByStripeIdAsync(string stripeSubscriptionId, DateTime endDate)
    {
        var subscription = await _db.Subscriptions
            .Where(s => s.StripeSubscriptionId == stripeSubscriptionId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription is null) return;

        subscription.Status = "Active";
        subscription.EndDate = endDate;
        await _db.SaveChangesAsync();
    }

    // ✅ Chamado pelo webhook customer.subscription.updated (canceled/unpaid) e customer.subscription.deleted
    public async Task DeactivateSubscriptionByStripeIdAsync(string stripeSubscriptionId)
    {
        var subscription = await _db.Subscriptions
            .Where(s => s.StripeSubscriptionId == stripeSubscriptionId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription is null) return;

        subscription.Status = "Canceled";
        subscription.EndDate = _clock.UtcNow;
        await _db.SaveChangesAsync();
    }
}