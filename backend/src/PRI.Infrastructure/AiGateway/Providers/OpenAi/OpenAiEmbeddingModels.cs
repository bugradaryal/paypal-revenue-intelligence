namespace PRI.Infrastructure.AiGateway.Providers.OpenAi;

// OpenAI Embeddings JSON modelleri (snake_case).

internal sealed record EmbeddingRequest(string Model, string Input, int? Dimensions, string EncodingFormat);

internal sealed class EmbeddingResponse
{
    public List<EmbeddingItem>? Data { get; set; }

    public EmbeddingUsage? Usage { get; set; }
}

internal sealed class EmbeddingItem
{
    public float[]? Embedding { get; set; }
}

internal sealed class EmbeddingUsage
{
    public int PromptTokens { get; set; }
}
