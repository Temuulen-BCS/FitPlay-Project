using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace FitPlay_Blazor.Auth;

public class AuthTokenMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiTokenHandler _tokenHandler;
    private readonly TokenStore _tokenStore;
    private readonly ILogger<AuthTokenMessageHandler> _logger;

    public AuthTokenMessageHandler(
        IHttpContextAccessor httpContextAccessor,
        ApiTokenHandler tokenHandler,
        TokenStore tokenStore,
        ILogger<AuthTokenMessageHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenHandler = tokenHandler;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? token = null;

        // 1) Try HttpContext (available during SSR / initial HTTP request)
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            try
            {
                token = _tokenHandler.CreateToken(user);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create JWT from HttpContext principal.");
            }
        }

        // 2) Fall back to the TokenStore (populated by middleware, survives the SignalR circuit)
        if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(_tokenStore.Token))
        {
            token = _tokenStore.Token;
        }

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("API request to {Url} sent without auth token — user is not authenticated or token could not be resolved.", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
