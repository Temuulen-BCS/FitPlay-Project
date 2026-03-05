using System.IO;
using System.Text;
using FitPlay.Api.Services;
using FitPlay.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/billing/webhook")]
public class BillingWebhookController : ControllerBase
{
    private readonly FitPlayContext _fitDb;
    private readonly MembershipService _membershipService;
    private readonly StripeOptions _stripeOptions;

    public BillingWebhookController(
        FitPlayContext fitDb,
        MembershipService membershipService,
        IOptions<StripeOptions> stripeOptions)
    {
        _fitDb = fitDb;
        _membershipService = membershipService;
        _stripeOptions = stripeOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var json = await new StreamReader(HttpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            return BadRequest(new { message = "Stripe WebhookSecret is not configured." });
        }

        Event stripeEvent;
        try
        {
            var signatureHeader = Request.Headers["Stripe-Signature"];
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _stripeOptions.WebhookSecret);
        }
        catch (Exception)
        {
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await HandleSubscriptionEvent(stripeEvent);
                break;
            case "invoice.paid":
            case "invoice.payment_failed":
                await HandleInvoiceEvent(stripeEvent);
                break;
        }

        return Ok();
    }

    private async Task HandleSubscriptionEvent(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null)
        {
            return;
        }

        var domainUser = await ResolveDomainUser(subscription.CustomerId);
        if (domainUser is null)
        {
            return;
        }

        var status = subscription.Status == "active" ? "Active" : "Inactive";
        DateTime? periodEnd = null;
        var periodEndValue = GetDateTimeValue(subscription, "CurrentPeriodEnd", "CurrentPeriodEndTimestamp");
        if (periodEndValue.HasValue)
        {
            periodEnd = periodEndValue;
        }

        await _membershipService.UpsertSubscriptionAsync(
            domainUser.Id,
            status,
            DateTime.UtcNow,
            periodEnd,
            subscription.CustomerId,
            subscription.Id);
    }

    private async Task HandleInvoiceEvent(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice is null)
        {
            return;
        }

        var domainUser = await ResolveDomainUser(invoice.CustomerId);
        if (domainUser is null)
        {
            return;
        }

        var status = stripeEvent.Type == "invoice.paid" ? "Active" : "PastDue";
        await _membershipService.UpsertSubscriptionAsync(
            domainUser.Id,
            status,
            DateTime.UtcNow,
            null,
            invoice.CustomerId,
            GetStringValue(invoice, "SubscriptionId", "Subscription"));
    }

    private async Task<FitPlay.Domain.Models.User?> ResolveDomainUser(string? customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        var subscription = await _fitDb.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeCustomerId == customerId);
        if (subscription is not null)
        {
            return await _fitDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == subscription.ClientId);
        }

        return null;
    }

    private static string? GetStringValue(object obj, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop is null)
            {
                continue;
            }

            var value = prop.GetValue(obj);
            if (value is string strValue)
            {
                return strValue;
            }

            if (value is not null)
            {
                var idProp = value.GetType().GetProperty("Id");
                if (idProp?.GetValue(value) is string idValue)
                {
                    return idValue;
                }
            }
        }

        return null;
    }

    private static DateTime? GetDateTimeValue(object obj, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            var prop = obj.GetType().GetProperty(name);
            if (prop is null)
            {
                continue;
            }

            var value = prop.GetValue(obj);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            if (value is long unixSeconds)
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }
        }

        return null;
    }
}
