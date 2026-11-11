namespace PRI.Domain.AiGateway;

/// <summary>
/// Token kullanımından USD maliyeti hesaplar. Fiyatlar sağlayıcının resmi fiyat listesinden gelir (ProviderModel),
/// hesap LLM'e bırakılmaz. Gizli akıl yürütme (reasoning) token'ları sağlayıcı tarafından çıktı token'ı olarak
/// raporlandığı için outputTokens içinde zaten vardır.
/// </summary>
public static class CostCalculator
{
    private const decimal TokensPerPriceUnit = 1_000_000m;
    private const int Scale = 8;

    /// <param name="inputTokens">Toplam girdi token'ı (önbellekten okunanlar dahil).</param>
    /// <param name="cachedInputTokens">Girdinin sağlayıcı önbelleğinden okunan kısmı.</param>
    /// <param name="cachedInputPricePerMillion">Önbellekli girdi fiyatı; sağlayıcı vermiyorsa null (normal girdi fiyatı uygulanır).</param>
    public static decimal Calculate(
        int inputTokens,
        int cachedInputTokens,
        int outputTokens,
        decimal inputPricePerMillion,
        decimal? cachedInputPricePerMillion,
        decimal outputPricePerMillion)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(cachedInputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(inputPricePerMillion);
        ArgumentOutOfRangeException.ThrowIfNegative(outputPricePerMillion);

        var cached = Math.Min(cachedInputTokens, inputTokens);
        var uncached = inputTokens - cached;
        var cachedRate = cachedInputPricePerMillion ?? inputPricePerMillion;

        var cost = (uncached * inputPricePerMillion + cached * cachedRate + outputTokens * outputPricePerMillion)
                   / TokensPerPriceUnit;

        return Math.Round(cost, Scale, MidpointRounding.AwayFromZero);
    }
}
