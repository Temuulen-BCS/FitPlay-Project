using FitPlay.Api.Auth;
using FitPlay.Api.Services;
using FitPlay.Domain.Data;
using FitPlay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly FitPlayContext _fitDb;
    private readonly MembershipService _membershipService;
    private readonly StripeOptions _stripeOptions;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClockService _clock;

    public BillingController(
        FitPlayContext fitDb,
        MembershipService membershipService,
        IOptions<StripeOptions> stripeOptions,
        UserManager<ApplicationUser> userManager,
        IClockService clock)
    {
        _fitDb = fitDb;
        _membershipService = membershipService;
        _stripeOptions = stripeOptions.Value;
        _userManager = userManager;
        _clock = clock;
    }

    public record MembershipStatusDto(bool IsActive, string Status, DateTime? CurrentPeriodEnd);
    public record CreateSubscriptionRequest(string ReturnUrl);
    public record CreateSubscriptionResponse(string ClientSecret);

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var identityUserId = _userManager.GetUserId(User);
        if (identityUserId is null) return Unauthorized();

        var domainUser = await _fitDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (domainUser is null)
            return Ok(new MembershipStatusDto(false, "None", null));

        var sub = await _membershipService.GetLatestSubscriptionAsync(domainUser.Id);

        if (sub is null)
            return Ok(new MembershipStatusDto(false, "None", null));

        return Ok(new MembershipStatusDto(sub.Status == "Active", sub.Status, sub.EndDate));
    }

    [HttpPost("create-subscription")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var identityUserId = _userManager.GetUserId(User);
        if (identityUserId is null) return Unauthorized();

        var domainUser = await _fitDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (domainUser is null)
            return BadRequest(new { message = "User not found." });

        if (string.IsNullOrWhiteSpace(_stripeOptions.PriceId))
            return BadRequest(new { message = "Stripe PriceId not configured." });

        // Ensure a Stripe customer exists for this user
        var existingSub = await _membershipService.GetLatestSubscriptionAsync(domainUser.Id);
        string? customerId = existingSub?.StripeCustomerId;

        if (string.IsNullOrWhiteSpace(customerId))
        {
            var identity = await _userManager.FindByIdAsync(identityUserId);
            var customerOptions = new CustomerCreateOptions
            {
                Email = identity?.Email
            };
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(customerOptions);
            customerId = customer.Id;
        }

        // Create the Stripe subscription with payment_behavior=default_incomplete
        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = _stripeOptions.PriceId }
            },
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = new List<string>
            {
                "latest_invoice",
                "latest_invoice.confirmation_secret"
            }
        };

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.CreateAsync(subscriptionOptions);

        // In Stripe.net v50, PaymentIntent was removed from Invoice.
        // The client secret is now accessed via Invoice.ConfirmationSecret.ClientSecret.

        var clientSecret = subscription.LatestInvoice?.ConfirmationSecret?.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret) && !string.IsNullOrWhiteSpace(subscription.LatestInvoiceId))
        {
            var invoiceService = new InvoiceService();
            var invoice = await invoiceService.GetAsync(
                subscription.LatestInvoiceId,
                new InvoiceGetOptions
                {
                    Expand = new List<string> { "confirmation_secret" }
                });
            clientSecret = invoice.ConfirmationSecret?.ClientSecret;
        }

        if (clientSecret is null)
            return StatusCode(500, new
            {
                message = "Failed to retrieve payment intent. Stripe did not return a confirmation secret.",
                stripeSubscriptionId = subscription.Id,
                stripeSubscriptionStatus = subscription.Status
            });

        await _membershipService.UpsertSubscriptionAsync(
            domainUser.Id,
            status: "Pending",
            startDate: _clock.UtcNow,
            endDate: null,
            stripeCustomerId: customerId,
            stripeSubscriptionId: subscription.Id);

        return Ok(new CreateSubscriptionResponse(clientSecret));
    }
}
