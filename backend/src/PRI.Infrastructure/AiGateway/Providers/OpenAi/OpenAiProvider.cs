using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.AiGateway.Providers.OpenAi;

/// <summary>OpenAI Chat Completions adaptörü. Maliyet hesabı burada yapılmaz; yalnızca gerçek token sayıları döndürülür.</summary>
public sealed class OpenAiProvider(
    HttpClient http,
    IOptions<OpenAiSettings> options,
    ILogger<OpenAiProvider> logger) : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AiProvider Provider => AiProvider.Openai;

    public async Task<ProviderCompletionResult> CompleteAsync(
        ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new BusinessRuleException(
                "openai_not_configured",
                "OpenAi:ApiKey is not configured (see README, 'OpenAI').");
        }

        // temperature yalnızca akıl yürütme kapalıyken geçerlidir; istenmişse reasoning_effort=none eklenir.
        var reasoningEffort = request.ReasoningEffort ?? (request.Temperature is not null ? "none" : null);

        var body = new ChatCompletionRequest(
            request.ModelId,
            request.Messages.Select(m => new ChatMessage(m.Role, m.Content)).ToList(),
            request.MaxOutputTokens,
            request.Temperature,
            reasoningEffort);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
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
            logger.LogWarning(ex, "OpenAI request failed to reach OpenAI.");
            throw new ExternalServiceException("provider_unavailable", "OpenAI could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("provider_unavailable", "The OpenAI request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await MapErrorAsync(response, cancellationToken);
            }

            var parsed = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var choice = parsed?.Choices?.FirstOrDefault();

            if (parsed?.Usage is null || choice is null)
            {
                throw new ExternalServiceException("provider_unavailable", "OpenAI returned an unexpected response.");
            }

            var inputTokens = parsed.Usage.PromptTokens;

            return new ProviderCompletionResult(
                choice.Message?.Content ?? string.Empty,
                inputTokens,
                Math.Min(parsed.Usage.PromptTokensDetails?.CachedTokens ?? 0, inputTokens),
                parsed.Usage.CompletionTokens,
                parsed.Id,
                choice.FinishReason);
        }
    }

    private async Task<Exception> MapErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var providerMessage = await TryReadErrorMessageAsync(response, cancellationToken);

        logger.LogWarning("OpenAI returned HTTP {Status}: {Message}", status, providerMessage ?? "(no message)");

        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                return new ExternalServiceException(
                    "provider_authentication_failed",
                    $"OpenAI rejected the API key (HTTP {status}). Check OpenAi:ApiKey.");

            case HttpStatusCode.TooManyRequests:
                return new ExternalServiceException(
                    "provider_rate_limited",
                    "OpenAI rate limit or quota reached (HTTP 429).");

            case HttpStatusCode.BadRequest:
            case HttpStatusCode.NotFound:
            case HttpStatusCode.UnprocessableEntity:
                return new BusinessRuleException(
                    "provider_rejected_request",
                    $"OpenAI rejected the request: {providerMessage ?? $"HTTP {status}"}");

            default:
                return new ExternalServiceException("provider_unavailable", $"OpenAI returned HTTP {status}.");
        }
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<OpenAiErrorEnvelope>(JsonOptions, cancellationToken);
            var message = envelope?.Error?.Message;

            return message is { Length: > 300 } ? message[..300] : message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
