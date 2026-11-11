namespace PRI.Application.AiGateway;

/// <summary>Semantic cache ayarları ("Gateway:Cache" bölümü).</summary>
public sealed class SemanticCacheSettings
{
    public const string SectionName = "Gateway:Cache";

    public bool Enabled { get; init; } = true;

    /// <summary>Anlamsal eşleşme için asgari kosinüs benzerliği. Yüksek tutmak yanlış eşleşme riskini azaltır.</summary>
    public double SimilarityThreshold { get; init; } = 0.93;

    public int TtlHours { get; init; } = 24;

    /// <summary>Benzerlik aramasında karşılaştırılacak en yeni aday sayısı (istemci + bağlam başına).</summary>
    public int MaxCandidates { get; init; } = 500;

    /// <summary>Bundan uzun sorgular için embedding alınmaz; yalnızca tam eşleşme aranır.</summary>
    public int MaxQueryCharacters { get; init; } = 8_000;

    public double EffectiveThreshold => Math.Clamp(SimilarityThreshold, 0.5d, 1d);

    public TimeSpan Ttl => TimeSpan.FromHours(Math.Max(TtlHours, 1));
}
