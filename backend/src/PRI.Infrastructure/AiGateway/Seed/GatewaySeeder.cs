using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.AiGateway.Seed;

/// <summary>
/// İlk first-party client'ı, model kataloglarını, baz modeli ve varsayılan yönlendirme kurallarını ekler
/// (yalnızca eksik olanları; mevcut kayıtlara dokunmaz).
/// Fiyatlar resmi sağlayıcı fiyat sayfalarından alınmıştır; kaynak ve kontrol tarihi kayıtla saklanır.
/// Fiyat değişirse yeni kayıt/migration ile güncellenir, uydurma veya tahmini fiyat kullanılmaz.
/// </summary>
public sealed class GatewaySeeder(AppDbContext db, TimeProvider timeProvider)
{
    private const string OpenAiPricingUrl = "https://developers.openai.com/api/docs/pricing";
    private static readonly DateOnly PricingCheckedOn = new(2026, 11, 7);

    private const string Luna = "gpt-6-luna";
    private const string Terra = "gpt-6.1-terra";
    private const string Sol = "gpt-6.1-sol";
    private const string Astra = "gpt-6-astra";

    // Tasarruf hesabının baz modeli: orta fiyatlı Standard model. Bilerek muhafazakâr seçildi (en pahalı model değil).
    private const string BaselineModelId = Terra;

    // (model id, görünen ad, katman, girdi, önbellekli girdi, çıktı) — USD / 1M token
    private static readonly (string ModelId, string Name, ModelTier Tier, decimal Input, decimal Cached, decimal Output)[] OpenAiModels =
    [
        (Luna, "GPT-6 Luna", ModelTier.Economy, 0.10m, 0.01m, 0.50m),
        (Terra, "GPT-6.1 Terra", ModelTier.Standard, 1.00m, 0.05m, 5.00m),
        (Sol, "GPT-6.1 Sol", ModelTier.Premium, 2.00m, 0.10m, 10.00m),
        (Astra, "GPT-6 Astra", ModelTier.Premium, 10.00m, 1.00m, 50.00m)
    ];

    // Varsayılan yönlendirme kuralları (görev tipi, ana model, yedekler). Yalnızca yapılandırmadır; kalite iddiası içermez.
    private static readonly (string TaskType, string Primary, string[] Fallbacks)[] DefaultRules =
    [
        (RoutingRule.DefaultTaskType, Luna, new[] { Terra }),
        ("classification", Luna, new[] { Terra }),
        ("summarization", Luna, new[] { Terra }),
        ("analysis", Terra, new[] { Sol })
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var unsupportedModels = await db.ProviderModels
            .Where(m => m.Provider != AiProvider.Openai && (m.IsEnabled || m.IsBaseline))
            .ToListAsync(cancellationToken);
        foreach (var model in unsupportedModels)
        {
            model.SetEnabled(false);
            model.SetBaseline(false);
        }

        if (!await db.GatewayClients.AnyAsync(c => c.IsFirstParty, cancellationToken))
        {
            db.GatewayClients.Add(GatewayClient.CreateFirstParty("PRI Dashboard", now));
        }

        var existingIds = await db.ProviderModels
            .Where(m => m.Provider == AiProvider.Openai)
            .Select(m => m.ModelId)
            .ToListAsync(cancellationToken);

        foreach (var seed in OpenAiModels.Where(m => !existingIds.Contains(m.ModelId)))
        {
            db.ProviderModels.Add(ProviderModel.Create(
                AiProvider.Openai, seed.ModelId, seed.Name, seed.Tier,
                seed.Input, seed.Cached, seed.Output, OpenAiPricingUrl, PricingCheckedOn, now));
        }

        await db.SaveChangesAsync(cancellationToken);

        var models = await db.ProviderModels
            .Where(m => m.Provider == AiProvider.Openai)
            .ToListAsync(cancellationToken);

        if (!models.Any(m => m.IsBaseline))
        {
            models.FirstOrDefault(m => m.ModelId == BaselineModelId)?.SetBaseline(true);
        }

        var existingRules = await db.RoutingRules.Select(r => r.TaskType).ToListAsync(cancellationToken);

        foreach (var rule in DefaultRules.Where(r => !existingRules.Contains(r.TaskType)))
        {
            var primary = models.FirstOrDefault(m => m.ModelId == rule.Primary);
            if (primary is null)
            {
                continue;
            }

            var fallbackIds = rule.Fallbacks
                .Select(id => models.FirstOrDefault(m => m.ModelId == id)?.Id)
                .OfType<Guid>();

            db.RoutingRules.Add(RoutingRule.Create(rule.TaskType, primary.Id, fallbackIds, now));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
