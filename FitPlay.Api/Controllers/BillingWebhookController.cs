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
        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
            return BadRequest(new { message = "Stripe WebhookSecret não configurado." });

        var json = await new StreamReader(HttpContext.Request.Body, Encoding.UTF8).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _stripeOptions.WebhookSecret,
                throwOnApiVersionMismatch: false); // necessário no Stripe.net v50+
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            // ✅ Principal: pagamento da invoice confirmado → ativa assinatura
            case "invoice.payment_succeeded":
                await HandleInvoicePaymentSucceeded(stripeEvent);
                break;

            // Fatura não paga → marca como PastDue
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailed(stripeEvent);
                break;

            // Assinatura atualizada pelo Stripe (renovação, cancelamento, etc.)
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await HandleSubscriptionEvent(stripeEvent);
                break;
        }

        return Ok();
    }

    // -------------------------------------------------------------------------
    // invoice.payment_succeeded
    // Disparado quando o pagamento da invoice é confirmado.
    // É o evento correto para ativar a assinatura — funciona tanto no
    // pagamento inicial quanto nas renovações mensais.
    // -------------------------------------------------------------------------
    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        // Busca a assinatura no Stripe para obter o período atual via line items
        var subService = new SubscriptionService();
        var stripeSub = await subService.GetAsync(subscriptionId, new SubscriptionGetOptions
        {
            Expand = new List<string> { "latest_invoice.lines" }
        });

        var periodEnd = stripeSub.LatestInvoice?.Lines?.Data
            .FirstOrDefault()?.Period?.End;

        var dbSub = await _fitDb.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
        if (dbSub is null) return;

        await _membershipService.UpsertSubscriptionAsync(
            dbSub.ClientId,
            status: "Active",
            startDate: DateTime.UtcNow,
            endDate: periodEnd,
            stripeCustomerId: invoice!.CustomerId,
            stripeSubscriptionId: subscriptionId);
    }

    // -------------------------------------------------------------------------
    // invoice.payment_failed
    // -------------------------------------------------------------------------
    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        var dbSub = await _fitDb.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
        if (dbSub is null) return;

        await _membershipService.UpsertSubscriptionAsync(
            dbSub.ClientId,
            status: "PastDue",
            startDate: dbSub.StartDate,
            endDate: dbSub.EndDate,
            stripeCustomerId: invoice!.CustomerId,
            stripeSubscriptionId: subscriptionId);
    }

    // -------------------------------------------------------------------------
    // customer.subscription.updated / deleted
    // -------------------------------------------------------------------------
    private async Task HandleSubscriptionEvent(Event stripeEvent)
    {
        var stripeSub = stripeEvent.Data.Object as Subscription;
        if (stripeSub is null) return;

        var dbSub = await _fitDb.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id);
        if (dbSub is null) return;

        var status = stripeSub.Status switch
        {
            "active" => "Active",
            "canceled" => "Canceled",
            "unpaid" => "Canceled",
            "past_due" => "PastDue",
            _ => "Inactive"
        };

        // CurrentPeriodEnd was removed in Stripe.net v50; derive from line items
        var periodEnd = stripeSub.LatestInvoice?.Lines?.Data
            .FirstOrDefault()?.Period?.End;

        await _membershipService.UpsertSubscriptionAsync(
            dbSub.ClientId,
            status: status,
            startDate: dbSub.StartDate,
            endDate: periodEnd,
            stripeCustomerId: stripeSub.CustomerId,
            stripeSubscriptionId: stripeSub.Id);
    }
}