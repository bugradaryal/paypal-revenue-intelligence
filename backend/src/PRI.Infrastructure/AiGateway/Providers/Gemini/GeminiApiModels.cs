using System.Text.Json.Serialization;

namespace PRI.Infrastructure.AiGateway.Providers.Gemini;

internal sealed record GeminiGenerateContentRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction? SystemInstruction,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

internal sealed record GeminiSystemInstruction(
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiContent(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiPart([property: JsonPropertyName("text")] string Text);

internal sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("maxOutputTokens")] int? MaxOutputTokens,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("thinkingConfig")] GeminiThinkingConfig? ThinkingConfig);

internal sealed record GeminiThinkingConfig([property: JsonPropertyName("thinkingLevel")] string ThinkingLevel);

internal sealed class GeminiGenerateContentResponse
{
    [JsonPropertyName("responseId")]
    public string? ResponseId { get; set; }

    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

internal sealed class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("cachedContentTokenCount")]
    public int CachedContentTokenCount { get; set; }
}

internal sealed class GeminiErrorEnvelope
{
    [JsonPropertyName("error")]
    public GeminiError? Error { get; set; }
}

internal sealed class GeminiError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
