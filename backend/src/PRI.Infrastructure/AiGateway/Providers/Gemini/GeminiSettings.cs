namespace PRI.Infrastructure.AiGateway.Providers.Gemini;

public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/";

    /// <summary>Secret, supplied through user-secrets or environment variables.</summary>
    public string ApiKey { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
