using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace FitPlay_Blazor.Auth;

public class AuthTokenMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiTokenHandler _tokenHandler;

    public AuthTokenMessageHandler(
        IHttpContextAccessor httpContextAccessor,
        ApiTokenHandler tokenHandler)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenHandler = tokenHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var token = _tokenHandler.CreateToken(user);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
