using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace PRI.Infrastructure.Revenue.PayPal;

public sealed class PayPalCredentialStore
{
    private const string ProtectorPurpose = "PRI.PayPalCredentials.v1";
    private readonly IDataProtector _protector;
    private readonly string _filePath;
    private readonly object _gate = new();
    private StoredCredentials? _stored;
    private readonly PayPalSettings _settings;

    public PayPalCredentialStore(
        IDataProtectionProvider protectionProvider,
        IOptions<PayPalSettings> settings)
    {
        _settings = settings.Value;
        _protector = protectionProvider.CreateProtector(ProtectorPurpose);
        _filePath = ResolvePath(_settings.CredentialStorePath);

        if (File.Exists(_filePath))
        {
            var protectedValue = File.ReadAllText(_filePath);
            var json = _protector.Unprotect(protectedValue);
            _stored = JsonSerializer.Deserialize<StoredCredentials>(json)
                ?? throw new InvalidOperationException("The encrypted PayPal credential file is empty or invalid.");
        }
    }

    public string ClientId
    {
        get { lock (_gate) return _stored?.ClientId ?? _settings.ClientId; }
    }

    public string ClientSecret
    {
        get { lock (_gate) return _stored?.ClientSecret ?? _settings.ClientSecret; }
    }

    public string WebhookId
    {
        get { lock (_gate) return _stored?.WebhookId ?? _settings.WebhookId; }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public async Task SaveAsync(string clientId, string clientSecret, string webhookId, CancellationToken cancellationToken)
    {
        var stored = new StoredCredentials(clientId, clientSecret, webhookId);
        var protectedValue = _protector.Protect(JsonSerializer.Serialize(stored));
        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("The PayPal credential store path has no parent directory.");
        Directory.CreateDirectory(directory);

        var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, protectedValue, cancellationToken);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            File.Move(tempPath, _filePath, true);
            lock (_gate) _stored = stored;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static string ResolvePath(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            throw new InvalidOperationException(
                "Set PayPal:CredentialStorePath because the current user's local application data directory is unavailable.");
        }

        return Path.Combine(localAppData, "PRI", "paypal-credentials.dat");
    }

    private sealed record StoredCredentials(string ClientId, string ClientSecret, string? WebhookId = null);
}
