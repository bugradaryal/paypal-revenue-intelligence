namespace PRI.Infrastructure.AiGateway.Providers.OpenAi;

// OpenAI Chat Completions JSON modelleri (snake_case, JsonNamingPolicy.SnakeCaseLower ile eşlenir).

internal sealed record ChatMessage(string Role, string Content);

/// <summary>max_tokens bu modellerde reddedilir; max_completion_tokens kullanılır (akıl yürütme token'larını da kapsar).</summary>
internal sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    int? MaxCompletionTokens,
    double? Temperature,
    string? ReasoningEffort);

internal sealed class ChatCompletionResponse
{
    public string? Id { get; set; }

    public List<ChatChoice>? Choices { get; set; }

    public ChatUsage? Usage { get; set; }
}

internal sealed class ChatChoice
{
    public ChatChoiceMessage? Message { get; set; }

    public string? FinishReason { get; set; }
}

internal sealed class ChatChoiceMessage
{
    public string? Content { get; set; }
}

internal sealed class ChatUsage
{
    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public PromptTokensDetails? PromptTokensDetails { get; set; }
}

internal sealed class PromptTokensDetails
{
    public int CachedTokens { get; set; }
}
