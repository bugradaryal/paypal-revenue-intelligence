using PRI.Shared.Constants;

namespace PRI.Infrastructure.Revenue.PayPal;

public sealed class PayPalSettings
{
    public const string SectionName = "PayPal";

    // Proje kuralı: yalnızca Sandbox. "live" ortamı bu sürümde bilerek desteklenmez.
    public const string SandboxBaseUrl = "https://api-m.sandbox.paypal.com";

    public string Environment { get; init; } = PayPalEnvironment.Sandbox;

    /// <summary>Gizli bilgiler user-secrets / environment variable ile verilir, repoya yazılmaz.</summary>
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>Optional location for encrypted PayPal credentials; defaults to the current user's local app data.</summary>
    public string CredentialStorePath { get; init; } = string.Empty;

    /// <summary>PayPal Developer Dashboard'da app'e webhook eklenince verilen Webhook ID (imza doğrulaması için).</summary>
    public string WebhookId { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
