namespace PRI.Infrastructure.AiGateway.Providers.OpenAi;

public sealed class OpenAiSettings
{
    public const string SectionName = "OpenAi";

    public const string DefaultBaseUrl = "https://api.openai.com/v1/";

    /// <summary>Gizli bilgi: user-secrets / environment variable ile verilir, repoya yazılmaz, frontend'e gönderilmez.</summary>
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = DefaultBaseUrl;

    /// <summary>Semantic cache aramaları için embedding modeli.</summary>
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    /// <summary>Embedding boyutu (text-embedding-3-* destekler). 0 veya negatif ise gönderilmez.</summary>
    public int EmbeddingDimensions { get; init; } = 256;

    /// <summary>
    /// 1 milyon embedding token'ı için USD. Bu değer yapılandırmadır: kullandığın modelin fiyatını OpenAI'ın resmi fiyat
    /// sayfasından doğrulayıp güncelle (embedding maliyeti ve bütçe hesabı buna dayanır).
    /// </summary>
    public decimal EmbeddingPricePerMillionUsd { get; init; } = 0.02m;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
