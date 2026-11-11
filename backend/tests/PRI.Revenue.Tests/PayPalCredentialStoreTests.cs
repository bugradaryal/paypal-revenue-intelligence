using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using PRI.Infrastructure.Revenue.PayPal;

namespace PRI.Revenue.Tests;

public sealed class PayPalCredentialStoreTests
{
    [Fact]
    public async Task Credentials_are_encrypted_on_disk_and_loaded_after_store_recreation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pri-paypal-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "credentials.dat");
        var options = Options.Create(new PayPalSettings { CredentialStorePath = path });
        var protectionProvider = new EphemeralDataProtectionProvider();

        try
        {
            var store = new PayPalCredentialStore(protectionProvider, options);
            await store.SaveAsync("sandbox-client-id", "sandbox-secret-value", "sandbox-webhook-id", CancellationToken.None);

            var protectedContents = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("sandbox-client-id", protectedContents, StringComparison.Ordinal);
            Assert.DoesNotContain("sandbox-secret-value", protectedContents, StringComparison.Ordinal);
            Assert.DoesNotContain("sandbox-webhook-id", protectedContents, StringComparison.Ordinal);

            var reloaded = new PayPalCredentialStore(protectionProvider, options);
            Assert.Equal("sandbox-client-id", reloaded.ClientId);
            Assert.Equal("sandbox-secret-value", reloaded.ClientSecret);
            Assert.Equal("sandbox-webhook-id", reloaded.WebhookId);
            Assert.True(reloaded.IsConfigured);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Legacy_credential_file_without_webhook_id_uses_configured_webhook_id()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pri-paypal-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "credentials.dat");
        var options = Options.Create(new PayPalSettings
        {
            CredentialStorePath = path,
            WebhookId = "configured-webhook-id"
        });
        var protectionProvider = new EphemeralDataProtectionProvider();

        try
        {
            Directory.CreateDirectory(directory);
            var protectedLegacyCredentials = protectionProvider
                .CreateProtector("PRI.PayPalCredentials.v1")
                .Protect("{\"ClientId\":\"legacy-client-id\",\"ClientSecret\":\"legacy-secret\"}");
            File.WriteAllText(path, protectedLegacyCredentials);

            var store = new PayPalCredentialStore(protectionProvider, options);

            Assert.Equal("legacy-client-id", store.ClientId);
            Assert.Equal("legacy-secret", store.ClientSecret);
            Assert.Equal("configured-webhook-id", store.WebhookId);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
