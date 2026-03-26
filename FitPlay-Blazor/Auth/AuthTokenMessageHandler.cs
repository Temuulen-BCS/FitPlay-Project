using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace FitPlay_Blazor.Auth;

public class AuthTokenMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiTokenHandler _tokenHandler;
    private readonly ILogger<AuthTokenMessageHandler> _logger;

    public AuthTokenMessageHandler(
        IHttpContextAccessor httpContextAccessor,
        ApiTokenHandler tokenHandler,
        ILogger<AuthTokenMessageHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenHandler = tokenHandler;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var token = _tokenHandler.CreateToken(user);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _logger.LogWarning("API request to {Url} sent without auth token — user is not authenticated.", request.RequestUri);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
