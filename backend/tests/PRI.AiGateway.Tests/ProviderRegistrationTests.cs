using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure;
using PRI.Infrastructure.AiGateway.Providers.Gemini;
using PRI.Infrastructure.AiGateway.Providers.OpenAi;

namespace PRI.AiGateway.Tests;

public sealed class ProviderRegistrationTests
{
    [Fact]
    public async Task Registers_openai_and_gemini_with_separate_base_addresses()
    {
        Uri? openAiUri = null;
        Uri? geminiUri = null;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "Host=localhost;Database=pri;Username=test;Password=test",
                ["OpenAi:ApiKey"] = "openai-test-key",
                ["Gemini:GeminiKey"] = "gemini-test-key"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddHttpClient<OpenAiProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(request =>
            {
                openAiUri = request.RequestUri;
                return JsonResponse("""
                    {"id":"openai-response","choices":[{"message":{"content":"hello"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1}}
                    """);
            }));
        services.AddHttpClient<GeminiProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(request =>
            {
                geminiUri = request.RequestUri;
                return JsonResponse("""
                    {"responseId":"gemini-response","candidates":[{"content":{"role":"model","parts":[{"text":"hello"}]},"finishReason":"STOP"}],"usageMetadata":{"promptTokenCount":1,"candidatesTokenCount":1}}
                    """);
            }));

        using var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<IAiProvider>().ToDictionary(provider => provider.Provider);
        var request = new ProviderCompletionRequest("test-model", [new ProviderMessage("user", "Hi")], null, null, null);

        await providers[AiProvider.Openai].CompleteAsync(request, CancellationToken.None);
        await providers[AiProvider.Gemini].CompleteAsync(request, CancellationToken.None);

        Assert.Equal("https://api.openai.com/v1/chat/completions", openAiUri?.ToString());
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/test-model:generateContent", geminiUri?.ToString());
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
