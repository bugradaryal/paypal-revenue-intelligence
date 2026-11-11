using PRI.Domain.AiGateway.Enums;
using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>Sağlayıcının bir modeli ve resmi token fiyatları. Fiyatın kaynağı ve kontrol tarihi her zaman saklanır.</summary>
public sealed class ProviderModel : BaseEntity
{
    // EF Core için.
    private ProviderModel()
    {
    }

    public AiProvider Provider { get; private set; }

    /// <summary>Sağlayıcı API'sine gönderilen model kimliği (ör. gpt-6-luna).</summary>
    public string ModelId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ModelTier Tier { get; private set; }

    /// <summary>1 milyon girdi token'ı için USD (kısa bağlam, Standard işleme).</summary>
    public decimal InputPricePerMillionUsd { get; private set; }

    /// <summary>1 milyon önbellekli girdi token'ı için USD; null ise normal girdi fiyatı uygulanır.</summary>
    public decimal? CachedInputPricePerMillionUsd { get; private set; }

    public decimal OutputPricePerMillionUsd { get; private set; }

    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Tasarruf hesabının karşılaştırma modeli. Otomatik yönlendirilen bir istek için "aynı token sayısı bu modelde
    /// olsaydı ne öderdik" hesaplanır. Aynı anda en fazla bir model baz modeldir.
    /// </summary>
    public bool IsBaseline { get; private set; }

    public string PricingSourceUrl { get; private set; } = string.Empty;

    /// <summary>Fiyatların kaynağıyla son karşılaştırıldığı tarih.</summary>
    public DateOnly PricingCheckedOn { get; private set; }

    public static ProviderModel Create(
        AiProvider provider,
        string modelId,
        string displayName,
        ModelTier tier,
        decimal inputPricePerMillionUsd,
        decimal? cachedInputPricePerMillionUsd,
        decimal outputPricePerMillionUsd,
        string pricingSourceUrl,
        DateOnly pricingCheckedOn,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pricingSourceUrl);
        ArgumentOutOfRangeException.ThrowIfNegative(inputPricePerMillionUsd);
        ArgumentOutOfRangeException.ThrowIfNegative(outputPricePerMillionUsd);

        if (cachedInputPricePerMillionUsd is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cachedInputPricePerMillionUsd), "Prices cannot be negative.");
        }

        return new ProviderModel
        {
            Provider = provider,
            ModelId = modelId.Trim(),
            DisplayName = displayName.Trim(),
            Tier = tier,
            InputPricePerMillionUsd = inputPricePerMillionUsd,
            CachedInputPricePerMillionUsd = cachedInputPricePerMillionUsd,
            OutputPricePerMillionUsd = outputPricePerMillionUsd,
            IsEnabled = true,
            PricingSourceUrl = pricingSourceUrl.Trim(),
            PricingCheckedOn = pricingCheckedOn,
            CreatedAtUtc = nowUtc
        };
    }

    public void SetEnabled(bool isEnabled) => IsEnabled = isEnabled;

    public void SetBaseline(bool isBaseline) => IsBaseline = isBaseline;

    public decimal CalculateCost(int inputTokens, int cachedInputTokens, int outputTokens) =>
        CostCalculator.Calculate(
            inputTokens, cachedInputTokens, outputTokens,
            InputPricePerMillionUsd, CachedInputPricePerMillionUsd, OutputPricePerMillionUsd);
}
