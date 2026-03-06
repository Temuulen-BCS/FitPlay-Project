using FitPlay.Api.Services;
using FitPlay.Domain.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitPlay_Blazor.Auth;

/// <summary>
/// Injects the "membership" claim into the Blazor Identity principal at request time
/// by querying the database directly. This enables the ActiveMembership policy to work
/// against the cookie-based principal used by the Blazor app.
/// </summary>
public class MembershipClaimsTransformation : IClaimsTransformation
{
    private readonly FitPlayContext _db;

    public MembershipClaimsTransformation(FitPlayContext db)
    {
        _db = db;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only process authenticated users who don't already have the membership claim
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim("membership", "active"))
            return principal;

        var identityId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityId))
            return principal;

        // Look up the domain user and their active subscription
        var domainUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityId);

        if (domainUser is null)
            return principal;

        var hasActiveMembership = await _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(s => s.ClientId == domainUser.Id && s.Status == "Active");

        if (!hasActiveMembership)
            return principal;

        // Clone the identity and add the membership claim
        var clonedIdentity = CloneIdentityWithClaim(principal.Identity, new Claim("membership", "active"));
        return new ClaimsPrincipal(clonedIdentity);
    }

    private static ClaimsIdentity CloneIdentityWithClaim(System.Security.Principal.IIdentity identity, Claim newClaim)
    {
        if (identity is ClaimsIdentity claimsIdentity)
        {
            var cloned = claimsIdentity.Clone();
            cloned.AddClaim(newClaim);
            return cloned;
        }

        var newIdentity = new ClaimsIdentity(identity);
        newIdentity.AddClaim(newClaim);
        return newIdentity;
    }
}
