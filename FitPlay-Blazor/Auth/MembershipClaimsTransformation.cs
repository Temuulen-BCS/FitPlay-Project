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
        // Only process authenticated users
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Skip if both claims are already present
        if (principal.HasClaim("membership", "active") && principal.HasClaim(c => c.Type == "full_name"))
            return principal;

        var identityId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityId))
            return principal;

        // Look up the domain user (regular user or trainer) and their active subscription
        var domainUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityId);

        var claimsToAdd = new List<Claim>();

        if (domainUser is not null)
        {
            // Regular user: inject full_name and check membership
            if (!string.IsNullOrWhiteSpace(domainUser.Name) && !principal.HasClaim("full_name", domainUser.Name))
                claimsToAdd.Add(new Claim("full_name", domainUser.Name));

            var hasActiveMembership = await _db.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.ClientId == domainUser.Id && s.Status == "Active");

            if (hasActiveMembership)
                claimsToAdd.Add(new Claim("membership", "active"));
        }
        else
        {
            // Fallback: check Teachers table (trainers don't have a domain User row)
            var teacher = await _db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == identityId);

            if (teacher is not null && !string.IsNullOrWhiteSpace(teacher.Name)
                && !principal.HasClaim("full_name", teacher.Name))
            {
                claimsToAdd.Add(new Claim("full_name", teacher.Name));
            }
        }

        if (claimsToAdd.Count == 0)
            return principal;

        // Clone the identity and add all new claims
        var clonedIdentity = principal.Identity is ClaimsIdentity ci ? ci.Clone() : new ClaimsIdentity(principal.Identity);
        foreach (var claim in claimsToAdd)
            clonedIdentity.AddClaim(claim);

        return new ClaimsPrincipal(clonedIdentity);
    }
}
