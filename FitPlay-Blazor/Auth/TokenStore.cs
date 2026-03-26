namespace FitPlay_Blazor.Auth;

/// <summary>
/// Scoped service that holds the JWT token for the lifetime of a Blazor circuit.
/// The token is captured during the initial SSR phase (when HttpContext is available)
/// and reused for all subsequent API calls over the SignalR connection.
/// </summary>
public class TokenStore
{
    public string? Token { get; set; }
}
