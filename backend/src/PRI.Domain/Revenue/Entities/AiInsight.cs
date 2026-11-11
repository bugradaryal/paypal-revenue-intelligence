using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

/// <summary>
/// Bir dönemin gelir içgörüsü. Tüm rakamlar C# ile hesaplanan <see cref="MetricsJson"/> anlık görüntüsündedir;
/// yapay zekâ yalnızca bu rakamlar üzerine yorum yazar (<see cref="Commentary"/>), yeni rakam üretmez.
/// </summary>
public sealed class AiInsight : BaseEntity
{
    // EF Core için.
    private AiInsight()
    {
    }

    public DateTime PeriodFromUtc { get; private set; }

    public DateTime PeriodToUtc { get; private set; }

    public string MetricsJson { get; private set; } = "{}";

    public string Commentary { get; private set; } = string.Empty;

    public string CommentaryLanguage { get; private set; } = "en";

    public ContentSource CommentarySource { get; private set; }

    public Guid? GatewayRequestId { get; private set; }

    public static AiInsight Create(
        DateTime periodFromUtc,
        DateTime periodToUtc,
        string metricsJson,
        string commentary,
        string commentaryLanguage,
        ContentSource commentarySource,
        Guid? gatewayRequestId,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricsJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(commentary);

        return new AiInsight
        {
            PeriodFromUtc = periodFromUtc,
            PeriodToUtc = periodToUtc,
            MetricsJson = metricsJson,
            Commentary = commentary.Trim(),
            CommentaryLanguage = commentaryLanguage,
            CommentarySource = commentarySource,
            GatewayRequestId = gatewayRequestId,
            CreatedAtUtc = nowUtc
        };
    }
}
