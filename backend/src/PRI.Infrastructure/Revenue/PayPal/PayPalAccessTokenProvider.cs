using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.Revenue.PayPal;

/// <summary>PayPal OAuth2 (client credentials) token'ını alır ve süresi dolana kadar bellekte tutar.</summary>
public sealed class PayPalAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    PayPalCredentialStore credentialStore,
    TimeProvider timeProvider,
    ILogger<PayPalAccessTokenProvider> logger)
{
    public const string HttpClientName = "PayPal.Auth";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTime _expiresAtUtc;

    /// <summary>PayPal 401 döndüğünde çağrılır; bir sonraki istek yeni token alır.</summary>
    public void Invalidate() => _token = null;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCachedToken(out cached))
            {
                return cached;
            }

            if (!credentialStore.IsConfigured)
            {
                throw new BusinessRuleException(
                    "paypal_not_configured",
                    "PayPal credentials have not been configured.");
            }

            return await RequestTokenAsync(credentialStore.ClientId, credentialStore.ClientSecret, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task ValidateCredentialsAsync(string clientId, string clientSecret, CancellationToken cancellationToken) =>
        RequestTokenAsync(clientId, clientSecret, cancellationToken);

    private bool TryGetCachedToken(out string token)
    {
        var current = _token;
        if (current is not null && timeProvider.GetUtcNow().UtcDateTime < _expiresAtUtc)
        {
            token = current;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task<string> RequestTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var http = httpClientFactory.CreateClient(HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PayPal token request failed to reach PayPal.");
            throw new ExternalServiceException("paypal_unavailable", "PayPal could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("paypal_unavailable", "The PayPal token request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PayPal token request returned HTTP {Status}.", (int)response.StatusCode);
                throw new ExternalServiceException(
                    "paypal_authentication_failed",
                    $"PayPal rejected the client credentials (HTTP {(int)response.StatusCode}). Check PayPal:ClientId / PayPal:ClientSecret and that they belong to a Sandbox app.");
            }

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken);
            if (string.IsNullOrWhiteSpace(body?.AccessToken))
            {
                throw new ExternalServiceException("paypal_unavailable", "PayPal returned an empty access token.");
            }

            // Süresi dolmadan 60 sn önce yenile.
            _expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(Math.Max(body.ExpiresIn - 60, 30));
            _token = body.AccessToken;

            return _token;
        }
    }
}
