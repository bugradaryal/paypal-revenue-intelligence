namespace PRI.Infrastructure.AiGateway.Providers.Gemini;

public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/";

    public string GeminiKey { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GeminiKey);
}
