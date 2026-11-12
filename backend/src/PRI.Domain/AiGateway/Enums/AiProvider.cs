namespace PRI.Domain.AiGateway.Enums;

/// <summary>
/// AI provider records. Anthropic remains for compatibility with existing database rows.
/// </summary>
public enum AiProvider
{
    Openai = 1,
    Anthropic = 2,
    Gemini = 3
}
