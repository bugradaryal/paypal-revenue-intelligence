using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PRI.Application.Revenue.DTOs;
using PRI.Infrastructure.Revenue.PayPal;

namespace PRI.Revenue.Tests;

public sealed class PayPalCredentialServiceTests
{
    [Fact]
    public async Task Rejected_credentials_are_not_saved_or_allowed_to_replace_existing_credentials()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pri-paypal-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "credentials.dat");
        var options = Options.Create(new PayPalSettings { CredentialStorePath = path });
        var store = new PayPalCredentialStore(new EphemeralDataProtectionProvider(), options);
        await store.SaveAsync("working-client", "working-secret", "working-webhook", CancellationToken.None);

        using var http = CreateHttpClient(HttpStatusCode.Unauthorized, "{}");
        var tokenProvider = new PayPalAccessTokenProvider(
            new StubHttpClientFactory(http),
            store,
            TimeProvider.System,
            NullLogger<PayPalAccessTokenProvider>.Instance);
        var service = new PayPalCredentialService(store, tokenProvider);

        try
        {
            await Assert.ThrowsAsync<PRI.Shared.Exceptions.ExternalServiceException>(() =>
                service.SaveAsync(
                    new SavePayPalCredentialsRequest("invalid-client", "invalid-secret", "new-webhook"),
                    CancellationToken.None));

            Assert.Equal("working-client", store.ClientId);
            Assert.Equal("working-secret", store.ClientSecret);
            Assert.Equal("working-webhook", store.WebhookId);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Accepted_credentials_are_saved_after_oauth_validation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pri-paypal-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "credentials.dat");
        var options = Options.Create(new PayPalSettings { CredentialStorePath = path });
        var store = new PayPalCredentialStore(new EphemeralDataProtectionProvider(), options);

        using var http = CreateHttpClient(
            HttpStatusCode.OK,
            """{"access_token":"validated-token","expires_in":3600}""");
        var tokenProvider = new PayPalAccessTokenProvider(
            new StubHttpClientFactory(http),
            store,
            TimeProvider.System,
            NullLogger<PayPalAccessTokenProvider>.Instance);
        var service = new PayPalCredentialService(store, tokenProvider);

        try
        {
            var result = await service.SaveAsync(
                new SavePayPalCredentialsRequest(" valid-client ", "valid-secret"),
                CancellationToken.None);

            Assert.True(result.Configured);
            Assert.Equal("valid-client", store.ClientId);
            Assert.Equal("valid-secret", store.ClientSecret);
            Assert.Empty(store.WebhookId);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content) =>
        new(new StubHttpMessageHandler(statusCode, content))
        {
            BaseAddress = new Uri(PayPalSettings.SandboxBaseUrl)
        };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }
}
