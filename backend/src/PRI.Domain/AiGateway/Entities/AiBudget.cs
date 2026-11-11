using PRI.Domain.AiGateway.Enums;
using PRI.Domain.Common;

namespace PRI.Domain.AiGateway.Entities;

/// <summary>
/// Aylık (UTC takvim ayı) AI harcama limiti. MVP'de tek kayıttır ve tüm istemcilerin toplam harcamasına uygulanır.
/// Harcama = tamamlama maliyeti + embedding maliyeti (gerçek kullanım).
/// </summary>
public sealed class AiBudget : BaseEntity
{
    public const int DefaultWarnAtPercent = 80;

    // EF Core için.
    private AiBudget()
    {
    }

    public decimal MonthlyLimitUsd { get; private set; }

    /// <summary>Harcama limitin bu yüzdesine ulaşınca seviye "warning" olur.</summary>
    public int WarnAtPercent { get; private set; }

    /// <summary>true ise limit aşıldığında yeni sağlayıcı çağrıları reddedilir; false ise yalnızca uyarı seviyesi değişir.</summary>
    public bool EnforceLimit { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static AiBudget Create(decimal monthlyLimitUsd, int warnAtPercent, bool enforceLimit, DateTime nowUtc)
    {
        var budget = new AiBudget { CreatedAtUtc = nowUtc };
        budget.Update(monthlyLimitUsd, warnAtPercent, enforceLimit, nowUtc);

        return budget;
    }

    public void Update(decimal monthlyLimitUsd, int warnAtPercent, bool enforceLimit, DateTime nowUtc)
    {
        if (monthlyLimitUsd <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyLimitUsd), "The budget limit must be greater than zero.");
        }

        if (warnAtPercent is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(warnAtPercent), "warnAtPercent must be between 1 and 100.");
        }

        MonthlyLimitUsd = monthlyLimitUsd;
        WarnAtPercent = warnAtPercent;
        EnforceLimit = enforceLimit;
        UpdatedAtUtc = nowUtc;
    }

    public BudgetLevel Evaluate(decimal spentUsd)
    {
        if (spentUsd >= MonthlyLimitUsd)
        {
            return BudgetLevel.Exceeded;
        }

        return spentUsd >= MonthlyLimitUsd * WarnAtPercent / 100m ? BudgetLevel.Warning : BudgetLevel.Ok;
    }

    /// <summary>Yeni bir sağlayıcı çağrısına izin verilmeli mi? (Çağrı başlamadan önceki harcamaya göre; tek istek limiti aşabilir.)</summary>
    public bool CanSpend(decimal spentUsd) => !EnforceLimit || Evaluate(spentUsd) != BudgetLevel.Exceeded;

    /// <summary>Verilen anı içeren UTC takvim ayı: [başlangıç, sonraki ayın başlangıcı).</summary>
    public static (DateTime StartUtc, DateTime EndUtc) PeriodContaining(DateTime nowUtc)
    {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return (start, start.AddMonths(1));
    }
}
