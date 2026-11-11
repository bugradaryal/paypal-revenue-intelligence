using PRI.Application.Common.DTOs;

namespace PRI.Application.AiGateway.DTOs;

/// <summary>Kullanım ve maliyet özeti. Tutarlar USD'dir, 6 ondalık basamaklıdır.</summary>
/// <param name="TotalRequests">Aralıkta tamamlanan (sağlayıcıdan yanıt alınan) istek sayısı; başarısız denemeler sayılmaz.</param>
/// <param name="InputTokens">Sağlayıcıya giden isteklerin toplam girdi token'ı.</param>
/// <param name="CacheHitRate">0..1. Aralıktaki cache aramalarının hit oranı (arama yoksa 0).</param>
/// <param name="Savings">Otomatik yönlendirilen isteklerde (baz model maliyeti − gerçek maliyet) toplamı. Negatif olabilir.</param>
/// <param name="Budget">Bütçe tanımlıysa içinde bulunulan ayın (UTC) limiti ve harcaması; aralıktan bağımsızdır. Tanımsızsa null.</param>
/// <param name="Cache">Aralıktaki cache aramaları.</param>
/// <param name="PromptOptimization">Aralıktaki prompt optimizasyonu etkisi (tahmini).</param>
public sealed record UsageSummaryDto(
    DateRangeDto Range,
    MoneyDto TotalCost,
    int TotalRequests,
    long InputTokens,
    long OutputTokens,
    double CacheHitRate,
    MoneyDto Savings,
    BudgetStatusDto? Budget,
    CacheSummaryDto Cache,
    PromptOptimizationSummaryDto PromptOptimization);

/// <param name="SavedCost">Hit'lerde kaçınılan maliyet (varsayım: yanıtın ilk üretim maliyeti).</param>
/// <param name="EmbeddingCost">Cache aramaları için ödenen gerçek embedding maliyeti. totalCost'a dahil değildir.</param>
public sealed record CacheSummaryDto(int Hits, int Misses, double HitRate, MoneyDto SavedCost, MoneyDto EmbeddingCost);

/// <param name="CharactersSaved">Ölçülmüş: kaldırılan toplam karakter.</param>
/// <param name="EstimatedTokensSaved">TAHMİN: karakter / 4.</param>
/// <param name="EstimatedSavings">TAHMİN: tahmini token × isteğin kullandığı modelin girdi fiyatı. Ölçülmüş "savings" ile toplanmaz.</param>
public sealed record PromptOptimizationSummaryDto(
    int OptimizedRequests,
    long CharactersSaved,
    long EstimatedTokensSaved,
    MoneyDto EstimatedSavings);

public sealed record BudgetStatusDto(MoneyDto Limit, MoneyDto Spent, string Period);

public sealed record UsageSeriesPointDto(DateTime Date, MoneyDto Cost, int Requests);

public sealed record UsageSeriesDto(DateRangeDto Range, IReadOnlyList<UsageSeriesPointDto> Items);

public sealed record UsageBreakdownItemDto(
    string Key,
    string Label,
    MoneyDto Cost,
    int Requests,
    long InputTokens,
    long OutputTokens);

public sealed record UsageBreakdownDto(
    DateRangeDto Range,
    IReadOnlyList<UsageBreakdownItemDto> ByModel,
    IReadOnlyList<UsageBreakdownItemDto> ByProvider);
