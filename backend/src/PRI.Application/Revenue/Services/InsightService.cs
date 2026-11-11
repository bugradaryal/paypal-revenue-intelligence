using System.Text.Json;
using System.Text.Json.Serialization;
using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Models;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Gelir içgörüsü: tüm rakamlar C# ile (veritabanı toplamları) hesaplanıp anlık görüntü olarak saklanır; yapay zekâ yalnızca
/// bu rakamlar üzerine kısa yorum yazar ve çıktısı doğrulanır. Yorum üretilemezse şablon yorum kullanılır.
/// </summary>
public sealed class InsightService(
    IRevenueIssueSync issueSync,
    IRevenueQueries queries,
    IRationaleWriter writer,
    IAiInsightRepository insights,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IInsightService
{
    private const string Usd = "USD";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<InsightDto> GenerateAsync(int days, string language, CancellationToken cancellationToken)
    {
        await issueSync.RefreshAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var to = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);   // bugün dahil
        var from = to.AddDays(-days);

        var current = await queries.GetInsightFactsAsync(from, to, cancellationToken);
        var previous = await queries.GetInsightFactsAsync(from.AddDays(-days), from, cancellationToken);
        var facts = current with
        {
            PeriodDays = days,
            PreviousRevenue = previous.Revenue,
            PreviousFailed = previous.Failed
        };

        var metrics = BuildMetrics(facts);
        var commentary = await writer.WriteInsightCommentaryAsync(facts, language, cancellationToken);

        var insight = AiInsight.Create(
            from, to, JsonSerializer.Serialize(metrics, JsonOptions),
            commentary.Text, language, commentary.Source, commentary.GatewayRequestId, now);
        insights.Add(insight);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(insight, metrics);
    }

    public async Task<PagedResult<InsightDto>> ListAsync(InsightListQuery query, CancellationToken cancellationToken)
    {
        var (items, total) = await insights.ListLatestAsync(query.Page, query.PageSize, cancellationToken);

        var dtos = items
            .Select(i => ToDto(i, JsonSerializer.Deserialize<InsightMetricsDto>(i.MetricsJson, JsonOptions)!))
            .ToList();

        return new PagedResult<InsightDto>(dtos, query.Page, query.PageSize, total);
    }

    private static InsightMetricsDto BuildMetrics(InsightFacts f)
    {
        var failureRate = f.PaymentCount == 0 ? 0d : Math.Round((double)f.FailedCount / f.PaymentCount, 4);
        var recoveryRate = f.Lost == 0 ? 0d : Math.Round((double)(f.Recovered / f.Lost), 4);

        return new InsightMetricsDto(
            MoneyDto.From(f.Revenue, Usd),
            MoneyDto.From(f.Failed, Usd),
            MoneyDto.From(f.Pending, Usd),
            f.PaymentCount,
            f.FailedCount,
            failureRate,
            MoneyDto.From(f.Lost, Usd),
            MoneyDto.From(f.Recovered, Usd),
            recoveryRate,
            f.OverdueCount,
            MoneyDto.From(f.OverdueAmount, Usd),
            f.TopReasons.Select(r => new FailureReasonDto(r.Code, r.Count, MoneyDto.From(r.Amount, Usd))).ToList(),
            new PreviousPeriodDto(
                MoneyDto.From(f.PreviousRevenue, Usd),
                MoneyDto.From(f.PreviousFailed, Usd),
                ChangePercent(f.Revenue, f.PreviousRevenue),
                ChangePercent(f.Failed, f.PreviousFailed)));
    }

    private static double? ChangePercent(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((double)((current - previous) / previous * 100m), 1);

    private static InsightDto ToDto(AiInsight insight, InsightMetricsDto metrics) =>
        new(insight.Id, insight.PeriodFromUtc, insight.PeriodToUtc, metrics, insight.Commentary,
            insight.CommentaryLanguage, insight.CommentarySource, insight.GatewayRequestId, insight.CreatedAtUtc);
}
