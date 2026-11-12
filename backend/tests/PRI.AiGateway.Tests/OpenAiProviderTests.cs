using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure.AiGateway.Providers.OpenAi;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public sealed class OpenAiProviderTests
{
    [Fact]
    public async Task Maps_plain_text_error_body_for_a_not_found_response()
    {
        const string providerMessage = "The selected model is not available to this project.";
        var provider = CreateProvider(new HttpClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(providerMessage, Encoding.UTF8, "text/plain")
            }))
        {
            BaseAddress = new Uri(OpenAiSettings.DefaultBaseUrl)
        });

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            provider.CompleteAsync(
                new ProviderCompletionRequest("gpt-6-luna", [new ProviderMessage("user", "Hi")], null, null, null),
                CancellationToken.None));

        Assert.Equal("provider_rejected_request", error.Code);
        Assert.Contains(providerMessage, error.Message);
    }

    [Fact]
    public async Task Keeps_json_provider_error_message()
    {
        const string providerMessage = "The selected model is not available to this project.";
        var provider = CreateProvider(new HttpClient(new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"error":{"message":"The selected model is not available to this project."}}""",
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri(OpenAiSettings.DefaultBaseUrl)
        });

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            provider.CompleteAsync(
                new ProviderCompletionRequest("gpt-6-luna", [new ProviderMessage("user", "Hi")], null, null, null),
                CancellationToken.None));

        Assert.Equal("provider_rejected_request", error.Code);
        Assert.Contains(providerMessage, error.Message);
    }

    private static OpenAiProvider CreateProvider(HttpClient http) =>
        new(http, Options.Create(new OpenAiSettings { ApiKey = "test-key" }), NullLogger<OpenAiProvider>.Instance);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
