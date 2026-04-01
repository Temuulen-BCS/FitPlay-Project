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
    private readonly ILogger<MembershipClaimsTransformation> _logger;

    public MembershipClaimsTransformation(FitPlayContext db, ILogger<MembershipClaimsTransformation> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Only process authenticated users
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var identityId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityId))
            return principal;

        try
        {
            // Look up the domain user (regular user or trainer) and their active subscription
            var domainUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityId);

            // Always clone so we can remove stale claims and add fresh ones
            var clonedIdentity = principal.Identity is ClaimsIdentity ci ? ci.Clone() : new ClaimsIdentity(principal.Identity);

            // Remove any existing membership claim so we always reflect current DB state
            var existingMembershipClaims = clonedIdentity.FindAll("membership").ToList();
            foreach (var c in existingMembershipClaims)
                clonedIdentity.RemoveClaim(c);

            if (domainUser is not null)
            {
                // Regular user: inject full_name and check membership
                if (!string.IsNullOrWhiteSpace(domainUser.Name) && !clonedIdentity.HasClaim("full_name", domainUser.Name))
                    clonedIdentity.AddClaim(new Claim("full_name", domainUser.Name));

                var hasActiveMembership = await _db.Subscriptions
                    .AsNoTracking()
                    .AnyAsync(s => s.ClientId == domainUser.Id && s.Status == "Active");

                if (hasActiveMembership)
                    clonedIdentity.AddClaim(new Claim("membership", "active"));
            }
            else
            {
                // Fallback: check Teachers table (trainers don't have a domain User row)
                var teacher = await _db.Teachers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdentityUserId == identityId);

                if (teacher is not null && !string.IsNullOrWhiteSpace(teacher.Name)
                    && !clonedIdentity.HasClaim("full_name", teacher.Name))
                {
                    clonedIdentity.AddClaim(new Claim("full_name", teacher.Name));
                }
            }

            return new ClaimsPrincipal(clonedIdentity);
        }
        catch (Exception ex)
        {
            // If the DB query fails (e.g. PostgreSQL timeout on Railway), do NOT let the
            // exception propagate through the auth middleware — that would make the user
            // appear unauthenticated and break every downstream API call.
            _logger.LogError(ex,
                "MembershipClaimsTransformation failed for user {IdentityId}. Returning unmodified principal.",
                identityId);
            return principal;
        }
    }
}
