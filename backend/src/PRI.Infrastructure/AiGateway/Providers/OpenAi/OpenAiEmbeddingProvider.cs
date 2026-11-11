using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.AiGateway.Providers.OpenAi;

/// <summary>
/// OpenAI Embeddings adaptörü (yalnızca semantic cache aramaları için). Maliyet = sağlayıcının bildirdiği gerçek token ×
/// OpenAi:EmbeddingPricePerMillionUsd. Fiyat yapılandırmadır ve resmi fiyat sayfasından doğrulanmalıdır.
/// </summary>
public sealed class OpenAiEmbeddingProvider(
    HttpClient http,
    IOptions<OpenAiSettings> options,
    ILogger<OpenAiEmbeddingProvider> logger) : IEmbeddingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new BusinessRuleException("openai_not_configured", "OpenAi:ApiKey is not configured (see README, 'OpenAI').");
        }

        var body = new EmbeddingRequest(
            settings.EmbeddingModel, text, settings.EmbeddingDimensions > 0 ? settings.EmbeddingDimensions : null, "float");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "embeddings")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenAI embeddings request failed to reach OpenAI.");
            throw new ExternalServiceException("provider_unavailable", "OpenAI could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("provider_unavailable", "The OpenAI embeddings request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                logger.LogWarning("OpenAI embeddings returned HTTP {Status}.", status);

                AppException failure = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        new ExternalServiceException("provider_authentication_failed", $"OpenAI rejected the API key (HTTP {status})."),
                    HttpStatusCode.TooManyRequests =>
                        new ExternalServiceException("provider_rate_limited", "OpenAI rate limit or quota reached (HTTP 429)."),
                    HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity =>
                        new BusinessRuleException(
                            "provider_rejected_request",
                            $"OpenAI rejected the embeddings request (HTTP {status}). Check OpenAi:EmbeddingModel / EmbeddingDimensions."),
                    _ => new ExternalServiceException("provider_unavailable", $"OpenAI returned HTTP {status}.")
                };

                throw failure;
            }

            var parsed = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(JsonOptions, cancellationToken);
            var vector = parsed?.Data?.FirstOrDefault()?.Embedding;

            if (vector is null || vector.Length == 0 || parsed?.Usage is null)
            {
                throw new ExternalServiceException("provider_unavailable", "OpenAI returned an unexpected embeddings response.");
            }

            var tokens = parsed.Usage.PromptTokens;
            var cost = CostCalculator.Calculate(tokens, 0, 0, settings.EmbeddingPricePerMillionUsd, null, 0m);

            return new EmbeddingResult(vector, tokens, cost, settings.EmbeddingModel);
        }
    }
}
