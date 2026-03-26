namespace FitPlay_Blazor.Auth;

/// <summary>
/// Middleware that runs on every request (during the initial SSR phase).
/// If the user is authenticated, it generates a JWT via ApiTokenHandler
/// and stores it in the scoped TokenStore so that subsequent API calls
/// over the SignalR circuit can attach the token.
/// </summary>
public class TokenCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public TokenCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenStore tokenStore, ApiTokenHandler tokenHandler)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                tokenStore.Token = tokenHandler.CreateToken(context.User);
            }
            catch
            {
                // Token creation may fail if claims are missing (e.g. NameIdentifier).
                // Proceed without a token — API calls will get 401 and the user will
                // see the "please log in" message.
            }
        }

        await _next(context);
    }
}
