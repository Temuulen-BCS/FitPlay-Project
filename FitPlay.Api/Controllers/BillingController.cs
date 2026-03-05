using System.Security.Claims;
using FitPlay.Api.Auth;
using FitPlay.Api.Services;
using FitPlay.Domain.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly FitPlayContext _fitDb;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MembershipService _membershipService;
    private readonly StripeOptions _stripeOptions;

    public BillingController(
        FitPlayContext fitDb,
        UserManager<ApplicationUser> userManager,
        MembershipService membershipService,
        IOptions<StripeOptions> stripeOptions)
    {
        _fitDb = fitDb;
        _userManager = userManager;
        _membershipService = membershipService;
        _stripeOptions = stripeOptions.Value;
    }

    public record BillingStatusDto(bool IsActive, string Status, DateTime? CurrentPeriodEnd);
    public record CreateSubscriptionRequest(string ReturnUrl);
    public record CreateSubscriptionResponse(string ClientSecret);

    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<BillingStatusDto>> GetStatus()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityUserId)) return Unauthorized();

        var domainUser = await _fitDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
        if (domainUser is null) return NotFound();

        var subscription = await _membershipService.GetLatestSubscriptionAsync(domainUser.Id);
        var isActive = subscription?.Status == "Active";

        return Ok(new BillingStatusDto(
            isActive,
            subscription?.Status ?? "None",
            subscription?.EndDate));
    }

    [HttpPost("create-subscription")]
    [Authorize]
    public async Task<ActionResult<CreateSubscriptionResponse>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(_stripeOptions.PriceId))
        {
            return BadRequest(new { message = "Stripe PriceId is not configured." });
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityUserId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user is null) return Unauthorized();

        var domainUser = await _fitDb.Users.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
        if (domainUser is null) return NotFound();

        var customerId = await EnsureCustomerAsync(user, domainUser.Id);

        var subscriptionService = new SubscriptionService();
        var options = new SubscriptionCreateOptions
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
            Expand = new List<string> { "latest_invoice" }
        };

        var subscription = await subscriptionService.CreateAsync(options);
        var invoiceId = GetStringValue(subscription, "LatestInvoiceId", "LatestInvoice");
        if (string.IsNullOrWhiteSpace(invoiceId))
        {
            return BadRequest(new { message = "Unable to initialize payment." });
        }

        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.GetAsync(invoiceId);
        var paymentIntentId = GetStringValue(invoice, "PaymentIntentId", "PaymentIntent");
        if (string.IsNullOrWhiteSpace(paymentIntentId))
        {
            return BadRequest(new { message = "Unable to initialize payment." });
        }

        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
        var clientSecret = paymentIntent.ClientSecret;
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return BadRequest(new { message = "Unable to initialize payment." });
        }

        await _membershipService.UpsertSubscriptionAsync(
            domainUser.Id,
            status: "Pending",
            startDate: DateTime.UtcNow,
            endDate: null,
            stripeCustomerId: customerId,
            stripeSubscriptionId: subscription.Id);

        return Ok(new CreateSubscriptionResponse(clientSecret));
    }

    private async Task<string> EnsureCustomerAsync(ApplicationUser user, int domainUserId)
    {
        var existing = await _membershipService.GetLatestSubscriptionAsync(domainUserId);
        if (!string.IsNullOrWhiteSpace(existing?.StripeCustomerId))
        {
            return existing.StripeCustomerId;
        }

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.UserName,
            Metadata = new Dictionary<string, string>
            {
                ["identityUserId"] = user.Id,
                ["domainUserId"] = domainUserId.ToString()
            }
        });

        return customer.Id;
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
}
