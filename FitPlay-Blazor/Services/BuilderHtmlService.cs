using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FitPlay.Blazor.Services;

public sealed class BuilderHtmlService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;

    public BuilderHtmlService(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _apiKey = config["Builder:ApiKey"] ?? string.Empty;
    }

    public async Task<string?> GetHtmlAsync(string model, string urlPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;
        if (string.IsNullOrWhiteSpace(model)) return null;
        if (string.IsNullOrWhiteSpace(urlPath)) return null;

        var cacheKey = $"builder-html:{model}:{urlPath}";
        if (_cache.TryGetValue(cacheKey, out string? cached)) return cached;

        var encodedPath = Uri.EscapeDataString(urlPath);
        var url = $"https://cdn.builder.io/api/v3/html/{model}?apiKey={_apiKey}&url={encodedPath}&format=json";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("html", out var htmlProp))
            {
                var html = htmlProp.GetString();
                if (!string.IsNullOrWhiteSpace(html))
                {
                    _cache.Set(cacheKey, html, TimeSpan.FromMinutes(3));
                    return html;
                }
            }
        }
        catch
        {
            // Swallow exceptions to allow fallback rendering
        }

        return null;
    }
}
