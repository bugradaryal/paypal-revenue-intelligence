using PRI.Domain.AiGateway.Enums;
using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>
/// Bir sağlayıcı çağrısının (denemenin) değiştirilemez kullanım kaydı. İstek anındaki fiyatlar kayda kopyalanır; sonradan
/// fiyat değişse bile geçmiş maliyet doğru kalır. İstek/yanıt içeriği saklanmaz.
/// </summary>
public sealed class UsageRecord : BaseEntity
{
    // EF Core için.
    private UsageRecord()
    {
    }

    public Guid ClientId { get; private set; }

    public Guid ProviderModelId { get; private set; }

    public AiProvider Provider { get; private set; }

    public string ModelId { get; private set; } = string.Empty;

    public UsageStatus Status { get; private set; }

    /// <summary>İstekteki görev tipi ipucu (küçük harf), yoksa null.</summary>
    public string? TaskType { get; private set; }

    /// <summary>Model otomatik yönlendirmeyle mi seçildi (true), yoksa istemci mi sabitledi (false).</summary>
    public bool IsAutoRouted { get; private set; }

    /// <summary>Bu isteğin kaçıncı denemesi (1 = ilk model, 2 = ilk yedek ...).</summary>
    public int Attempt { get; private set; }

    public int InputTokens { get; private set; }

    public int CachedInputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    /// <summary>Gerçek token sayısı × istek anındaki resmi fiyat.</summary>
    public decimal CostUsd { get; private set; }

    /// <summary>Otomatik yönlendirilen istekte baz modelin kimliği; yoksa null (tasarruf iddia edilmez).</summary>
    public string? BaselineModelId { get; private set; }

    /// <summary>Aynı token sayısı baz modelde olsaydı oluşacak maliyet (varsayım: token sayısı modelden bağımsız).</summary>
    public decimal? BaselineCostUsd { get; private set; }

    public decimal InputPricePerMillionUsd { get; private set; }

    public decimal? CachedInputPricePerMillionUsd { get; private set; }

    public decimal OutputPricePerMillionUsd { get; private set; }

    public int LatencyMs { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ProviderRequestId { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    /// <summary>Prompt optimizasyonunun kaldırdığı karakter sayısı (ölçülmüş). Token karşılığı yalnızca tahmindir; gerçek token sayısı sağlayıcıdan gelir.</summary>
    public int PromptCharsSaved { get; private set; }

    public static UsageRecord Success(
        Guid clientId,
        ProviderModel model,
        string? taskType,
        bool isAutoRouted,
        int attempt,
        int inputTokens,
        int cachedInputTokens,
        int outputTokens,
        ProviderModel? baselineModel,
        int latencyMs,
        string? providerRequestId,
        DateTime requestedAtUtc,
        DateTime nowUtc,
        int promptCharsSaved = 0)
    {
        var record = Base(clientId, model, UsageStatus.Success, taskType, isAutoRouted, attempt, latencyMs, requestedAtUtc, nowUtc);

        record.InputTokens = inputTokens;
        record.CachedInputTokens = Math.Min(cachedInputTokens, inputTokens);
        record.OutputTokens = outputTokens;
        record.ProviderRequestId = providerRequestId;
        record.PromptCharsSaved = Math.Max(promptCharsSaved, 0);
        record.CostUsd = model.CalculateCost(inputTokens, cachedInputTokens, outputTokens);

        // Tasarruf yalnızca otomatik yönlendirilen ve baz modeli bilinen isteklerde iddia edilir.
        if (isAutoRouted && baselineModel is not null)
        {
            record.BaselineModelId = baselineModel.ModelId;
            record.BaselineCostUsd = baselineModel.CalculateCost(inputTokens, cachedInputTokens, outputTokens);
        }

        return record;
    }

    /// <summary>Sağlayıcıdan yanıt alınamadı; kullanım ve maliyet 0 kabul edilir.</summary>
    public static UsageRecord Failure(
        Guid clientId,
        ProviderModel model,
        string? taskType,
        bool isAutoRouted,
        int attempt,
        string errorCode,
        int latencyMs,
        DateTime requestedAtUtc,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        var record = Base(clientId, model, UsageStatus.Failed, taskType, isAutoRouted, attempt, latencyMs, requestedAtUtc, nowUtc);
        record.ErrorCode = errorCode;

        return record;
    }

    private static UsageRecord Base(
        Guid clientId,
        ProviderModel model,
        UsageStatus status,
        string? taskType,
        bool isAutoRouted,
        int attempt,
        int latencyMs,
        DateTime requestedAtUtc,
        DateTime nowUtc) =>
        new()
        {
            ClientId = clientId,
            ProviderModelId = model.Id,
            Provider = model.Provider,
            ModelId = model.ModelId,
            Status = status,
            TaskType = taskType,
            IsAutoRouted = isAutoRouted,
            Attempt = Math.Max(attempt, 1),
            InputPricePerMillionUsd = model.InputPricePerMillionUsd,
            CachedInputPricePerMillionUsd = model.CachedInputPricePerMillionUsd,
            OutputPricePerMillionUsd = model.OutputPricePerMillionUsd,
            LatencyMs = Math.Max(latencyMs, 0),
            RequestedAtUtc = requestedAtUtc,
            CreatedAtUtc = nowUtc
        };
}
