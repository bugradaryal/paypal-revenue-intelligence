using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.Revenue.PayPal;

/// <summary>PayPal'ın verify-webhook-signature API'sini çağırır. Olay JSON'u, imzanın bozulmaması için ham haliyle gömülür.</summary>
public sealed class PayPalWebhookVerifier(
    HttpClient http,
    PayPalAccessTokenProvider tokenProvider,
    PayPalCredentialStore credentialStore,
    ILogger<PayPalWebhookVerifier> logger) : IPayPalWebhookVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<bool> VerifyAsync(PayPalWebhookHeaders headers, string rawEventJson, CancellationToken cancellationToken)
    {
        var webhookId = credentialStore.WebhookId;
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            throw new BusinessRuleException(
                "paypal_not_configured",
                "PayPal:WebhookId is not configured (see README, 'PayPal Sandbox').");
        }

        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        var body = "{"
            + $"\"auth_algo\":{Json(headers.AuthAlgo)},"
            + $"\"cert_url\":{Json(headers.CertUrl)},"
            + $"\"transmission_id\":{Json(headers.TransmissionId)},"
            + $"\"transmission_sig\":{Json(headers.TransmissionSig)},"
            + $"\"transmission_time\":{Json(headers.TransmissionTime)},"
            + $"\"webhook_id\":{Json(webhookId)},"
            + $"\"webhook_event\":{rawEventJson}"
            + "}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/notifications/verify-webhook-signature")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "PayPal webhook verification failed to reach PayPal.");
            throw new ExternalServiceException("paypal_unavailable", "PayPal could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("paypal_unavailable", "The PayPal request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PayPal webhook verification returned HTTP {Status}.", (int)response.StatusCode);
                throw new ExternalServiceException(
                    "paypal_unavailable",
                    $"PayPal returned HTTP {(int)response.StatusCode} while verifying the webhook.");
            }

            var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(JsonOptions, cancellationToken);

            return string.Equals(result?.VerificationStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string Json(string value) => JsonSerializer.Serialize(value);

    private sealed class VerifyResponse
    {
        public string? VerificationStatus { get; set; }
    }
}
