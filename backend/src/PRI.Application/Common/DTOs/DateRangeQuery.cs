namespace PRI.Application.Common.DTOs;

/// <summary>Özet, seri ve dağılım uçlarının ortak sorgusu. from dahil, to hariçtir.</summary>
public sealed class DateRangeQuery
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}
