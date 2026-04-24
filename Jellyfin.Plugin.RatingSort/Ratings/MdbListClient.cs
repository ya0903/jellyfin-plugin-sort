using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RatingSort.Ratings;

public sealed class MdbListClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MdbListClient> _logger;

    public MdbListClient(IHttpClientFactory httpClientFactory, ILogger<MdbListClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    internal async Task<MdbListLookupResult> GetByTmdbAsync(string contentType, string tmdbId, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(tmdbId) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new MdbListLookupResult();
        }

        var url = $"https://api.mdblist.com/tmdb/{Uri.EscapeDataString(contentType)}/{Uri.EscapeDataString(tmdbId)}?apikey={Uri.EscapeDataString(apiKey)}";

        try
        {
            var http = _httpClientFactory.CreateClient(nameof(MdbListClient));
            http.Timeout = TimeSpan.FromSeconds(20);

            using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var remaining = TryGetIntHeader(response, "X-RateLimit-Remaining");
            var resetUtc = TryGetResetUtc(response, "X-RateLimit-Reset");
            var rateLimited = (int)response.StatusCode == 429;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MDBList request failed with {StatusCode} for {ContentType} {TmdbId}", (int)response.StatusCode, contentType, tmdbId);
                return new MdbListLookupResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsRateLimited = rateLimited,
                    RateLimitRemaining = remaining,
                    RateLimitResetUtc = resetUtc
                };
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<MdbListTitleResponse>(raw, JsonOptions);

            return new MdbListLookupResult
            {
                StatusCode = (int)response.StatusCode,
                IsRateLimited = remaining.HasValue && remaining.Value <= 0,
                RateLimitRemaining = remaining,
                RateLimitResetUtc = resetUtc,
                Data = data
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MDBList request failed for {ContentType} {TmdbId}", contentType, tmdbId);
            return new MdbListLookupResult();
        }
    }

    private static int? TryGetIntHeader(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            && int.TryParse(values.FirstOrDefault(), out var parsed)
                ? parsed
                : null;
    }

    private static DateTimeOffset? TryGetResetUtc(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values)
            && long.TryParse(values.FirstOrDefault(), out var seconds)
            && seconds > 0
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
    }
}
