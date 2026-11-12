using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.AiGateway.Providers.Gemini;

/// <summary>Gemini generateContent adapter.</summary>
public sealed class GeminiProvider(
    HttpClient http,
    IOptions<GeminiSettings> options,
    ILogger<GeminiProvider> logger) : IAiProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiProvider Provider => AiProvider.Gemini;

    public async Task<ProviderCompletionResult> CompleteAsync(
        ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new BusinessRuleException("gemini_not_configured", "Gemini:ApiKey is not configured.");
        }

        var systemText = string.Join(
            "\n\n",
            request.Messages.Where(message => message.Role == "system").Select(message => message.Content));
        var contents = request.Messages
            .Where(message => message.Role != "system")
            .Select(message => new GeminiContent(
                message.Role == "assistant" ? "model" : "user",
                [new GeminiPart(message.Content)]))
            .ToList();
        var thinkingLevel = ToThinkingLevel(request.ReasoningEffort);
        var body = new GeminiGenerateContentRequest(
            contents,
            string.IsNullOrWhiteSpace(systemText) ? null : new GeminiSystemInstruction([new GeminiPart(systemText)]),
            new GeminiGenerationConfig(
                request.MaxOutputTokens,
                request.Temperature,
                thinkingLevel is null ? null : new GeminiThinkingConfig(thinkingLevel)));

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(request.ModelId)}:generateContent")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        httpRequest.Headers.Add("x-goog-api-key", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(httpRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Gemini request failed to reach Google.");
            throw new ExternalServiceException("provider_unavailable", "Gemini could not be reached.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("provider_unavailable", "The Gemini request timed out.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await MapErrorAsync(response, cancellationToken);
            }

            var parsed = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(JsonOptions, cancellationToken);
            var candidate = parsed?.Candidates?.FirstOrDefault();
            if (parsed?.UsageMetadata is null || candidate is null)
            {
                throw new ExternalServiceException("provider_unavailable", "Gemini returned an unexpected response.");
            }

            var content = string.Concat(candidate.Content?.Parts?.Select(part => part.Text) ?? []);
            var inputTokens = parsed.UsageMetadata.PromptTokenCount;

            return new ProviderCompletionResult(
                content,
                inputTokens,
                Math.Min(parsed.UsageMetadata.CachedContentTokenCount, inputTokens),
                parsed.UsageMetadata.CandidatesTokenCount,
                parsed.ResponseId,
                candidate.FinishReason);
        }
    }

    private async Task<Exception> MapErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        string? message = null;
        try
        {
            message = (await response.Content.ReadFromJsonAsync<GeminiErrorEnvelope>(JsonOptions, cancellationToken))?.Error?.Message;
            if (message is { Length: > 300 })
            {
                message = message[..300];
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Some provider errors do not return a JSON envelope.
        }

        logger.LogWarning("Gemini returned HTTP {Status}: {Message}", status, message ?? "(no message)");

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new ExternalServiceException(
                    "provider_authentication_failed",
                    $"Gemini rejected the API key (HTTP {status}). Check Gemini:ApiKey."),
            HttpStatusCode.TooManyRequests =>
                new ExternalServiceException("provider_rate_limited", $"Gemini rate limit or quota reached (HTTP {status})."),
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity =>
                new BusinessRuleException(
                    "provider_rejected_request",
                    $"Gemini rejected the request: {message ?? $"HTTP {status}"}"),
            _ => new ExternalServiceException("provider_unavailable", $"Gemini returned HTTP {status}.")
        };
    }

    private static string? ToThinkingLevel(string? effort) =>
        effort?.Trim().ToLowerInvariant() switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" or "xhigh" => "high",
            _ => null
        };
}
