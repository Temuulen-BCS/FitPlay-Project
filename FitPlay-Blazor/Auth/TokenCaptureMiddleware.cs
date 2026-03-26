namespace FitPlay_Blazor.Auth;

/// <summary>
/// Middleware that runs on every HTTP request (after UseAuthentication).
/// If the user is authenticated, it stores the ClaimsPrincipal in the
/// scoped TokenStore so that subsequent API calls over the SignalR circuit
/// can generate fresh JWTs from it.
/// </summary>
public class TokenCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public TokenCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenStore tokenStore)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            tokenStore.Principal = context.User;
        }

        await _next(context);
    }
}
