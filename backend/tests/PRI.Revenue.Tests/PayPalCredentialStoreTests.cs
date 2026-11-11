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
            await store.SaveAsync("sandbox-client-id", "sandbox-secret-value", CancellationToken.None);

            var protectedContents = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("sandbox-client-id", protectedContents, StringComparison.Ordinal);
            Assert.DoesNotContain("sandbox-secret-value", protectedContents, StringComparison.Ordinal);

            var reloaded = new PayPalCredentialStore(protectionProvider, options);
            Assert.Equal("sandbox-client-id", reloaded.ClientId);
            Assert.Equal("sandbox-secret-value", reloaded.ClientSecret);
            Assert.True(reloaded.IsConfigured);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
