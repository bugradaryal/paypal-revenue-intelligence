using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure.AiGateway.Providers.Gemini;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public sealed class GeminiProviderTests
{
    [Fact]
    public async Task Sends_gemini_request_and_maps_text_and_usage()
    {
        string? capturedUri = null;
        string? capturedApiKey = null;
        string? capturedBody = null;
        var http = new HttpClient(new StubHandler(request =>
        {
            capturedUri = request.RequestUri?.ToString();
            capturedApiKey = request.Headers.GetValues("x-goog-api-key").Single();
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(HttpStatusCode.OK, """
                {
                  "responseId": "response-123",
                  "candidates": [{
                    "content": { "role": "model", "parts": [{ "text": "Hello " }, { "text": "there" }] },
                    "finishReason": "STOP"
                  }],
                  "usageMetadata": {
                    "promptTokenCount": 20,
                    "candidatesTokenCount": 8,
                    "cachedContentTokenCount": 5
                  }
                }
                """);
        }))
        {
            BaseAddress = new Uri(GeminiSettings.DefaultBaseUrl)
        };
        var provider = CreateProvider(http);

        var result = await provider.CompleteAsync(
            new ProviderCompletionRequest(
                "gemini-3.5-flash-lite",
                [
                    new ProviderMessage("system", "Be concise."),
                    new ProviderMessage("user", "Hi"),
                    new ProviderMessage("assistant", "Hello"),
                    new ProviderMessage("user", "How are you?")
                ],
                300,
                0.4,
                "high"),
            CancellationToken.None);

        Assert.Equal(AiProvider.Gemini, provider.Provider);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent", capturedUri);
        Assert.Equal("test-key", capturedApiKey);
        using var requestBody = JsonDocument.Parse(capturedBody!);
        Assert.Equal("Be concise.", requestBody.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("model", requestBody.RootElement.GetProperty("contents")[1].GetProperty("role").GetString());
        Assert.Equal("high", requestBody.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString());
        Assert.Equal("Hello there", result.Content);
        Assert.Equal(20, result.InputTokens);
        Assert.Equal(5, result.CachedInputTokens);
        Assert.Equal(8, result.OutputTokens);
        Assert.Equal("response-123", result.ProviderRequestId);
        Assert.Equal("STOP", result.FinishReason);
    }

    [Fact]
    public async Task Maps_rate_limit_for_gateway_fallback_handling()
    {
        var provider = CreateProvider(new HttpClient(new StubHandler(_ =>
            JsonResponse(HttpStatusCode.TooManyRequests, """{"error":{"message":"quota exceeded"}}""")))
        {
            BaseAddress = new Uri(GeminiSettings.DefaultBaseUrl)
        });

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            provider.CompleteAsync(
                new ProviderCompletionRequest("gemini-3.5-flash-lite", [new ProviderMessage("user", "Hi")], null, null, null),
                CancellationToken.None));

        Assert.Equal("provider_rate_limited", error.Code);
    }

    [Fact]
    public async Task Rejects_unconfigured_provider_before_sending_request()
    {
        var provider = new GeminiProvider(
            new HttpClient(new StubHandler(_ => throw new InvalidOperationException("Request should not be sent."))),
            Options.Create(new GeminiSettings()),
            NullLogger<GeminiProvider>.Instance);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            provider.CompleteAsync(
                new ProviderCompletionRequest("gemini-3.5-flash-lite", [new ProviderMessage("user", "Hi")], null, null, null),
                CancellationToken.None));

        Assert.Equal("gemini_not_configured", error.Code);
    }

    private static GeminiProvider CreateProvider(HttpClient http) =>
        new(http, Options.Create(new GeminiSettings { GeminiKey = "test-key" }), NullLogger<GeminiProvider>.Instance);

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
