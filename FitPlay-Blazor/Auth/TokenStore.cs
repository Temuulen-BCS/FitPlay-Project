using System.Security.Claims;

namespace FitPlay_Blazor.Auth;

/// <summary>
/// Scoped service that holds the authenticated ClaimsPrincipal for the lifetime
/// of a Blazor circuit. The principal is captured during the initial SSR phase
/// (when HttpContext is available) and used to generate fresh JWTs for each
/// API call over the SignalR connection — avoiding token expiry issues.
/// </summary>
public class TokenStore
{
    public ClaimsPrincipal? Principal { get; set; }
}
