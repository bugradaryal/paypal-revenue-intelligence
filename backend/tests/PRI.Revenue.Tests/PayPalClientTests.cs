using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PRI.Infrastructure.Revenue.PayPal;
using PRI.Shared.Exceptions;

namespace PRI.Revenue.Tests;

public sealed class PayPalClientTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Transaction_search_refreshes_token_once_after_forbidden(bool retrySucceeds)
    {
        var tokenRequestCount = 0;
        var apiRequestTokens = new List<string>();
        var settings = new PayPalSettings
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            CredentialStorePath = Path.Combine(Path.GetTempPath(), $"pri-paypal-{Guid.NewGuid():N}", "credentials.dat")
        };
        var settingsOptions = Options.Create(settings);
        var credentialStore = new PayPalCredentialStore(new EphemeralDataProtectionProvider(), settingsOptions);
        var authClient = new HttpClient(new StubHandler(_ =>
        {
            tokenRequestCount++;
            return JsonResponse(
                HttpStatusCode.OK,
                $"{{\"access_token\":\"token-{tokenRequestCount}\",\"expires_in\":3600}}");
        }))
        {
            BaseAddress = new Uri($"{PayPalSettings.SandboxBaseUrl}/")
        };
        var tokenProvider = new PayPalAccessTokenProvider(
            new StubHttpClientFactory(authClient),
            credentialStore,
            TimeProvider.System,
            NullLogger<PayPalAccessTokenProvider>.Instance);
        var apiRequestCount = 0;
        var apiClient = new HttpClient(new StubHandler(request =>
        {
            apiRequestCount++;
            apiRequestTokens.Add(request.Headers.Authorization?.Parameter ?? string.Empty);
            return apiRequestCount == 1 || !retrySucceeds
                ? new HttpResponseMessage(HttpStatusCode.Forbidden)
                : JsonResponse(HttpStatusCode.OK, "{\"transaction_details\":[],\"total_pages\":1}");
        }))
        {
            BaseAddress = new Uri($"{PayPalSettings.SandboxBaseUrl}/")
        };
        var payPalClient = new PayPalClient(
            apiClient,
            tokenProvider,
            credentialStore,
            settingsOptions,
            NullLogger<PayPalClient>.Instance,
            TimeProvider.System);

        if (retrySucceeds)
        {
            await payPalClient.TestConnectionAsync(CancellationToken.None);
        }
        else
        {
            var error = await Assert.ThrowsAsync<ExternalServiceException>(
                () => payPalClient.TestConnectionAsync(CancellationToken.None));
            Assert.Equal("paypal_transaction_search_forbidden", error.Code);
        }

        Assert.Equal(2, tokenRequestCount);
        Assert.Equal(2, apiRequestCount);
        Assert.Equal(new[] { "token-1", "token-2" }, apiRequestTokens);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
