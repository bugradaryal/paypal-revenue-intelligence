using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PRI.Application.Common;
using PRI.Application.Common.Interfaces;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Revenue.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Öneri gerekçesi ve insight yorumunu yazar. Rakamların hepsi C# ile hesaplanıp olgu olarak verilir; modelden yalnızca
/// anlatım istenir ve çıktı <see cref="NumberGuard"/> ile doğrulanır. Müşteri adı/e-postası modele gönderilmez.
/// AI kapalı, bütçe dolu, hata veya doğrulama başarısızsa şablon metin kullanılır; istek bu yüzden asla başarısız olmaz.
/// </summary>
public sealed class RationaleWriter(IAiGateway gateway, ILogger<RationaleWriter> logger) : IRationaleWriter
{
    private const int MaxLength = 600;

    public async Task<WrittenText> WriteRationaleAsync(RationaleFacts facts, string language, CancellationToken cancellationToken)
    {
        var fallback = RationaleTemplates.Build(
            facts.ActionType, facts.Amount, facts.Currency, facts.FailureCode, facts.DaysOutstanding, language);

        var factText = new StringBuilder()
            .AppendLine($"issue: {facts.IssueType}")
            .AppendLine($"recommended action: {facts.ActionType}")
            .AppendLine($"amount: {facts.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {facts.Currency}")
            .AppendLine($"days outstanding: {facts.DaysOutstanding}");
        if (facts.FailureCode is { } code)
        {
            factText.AppendLine($"failure reason: {code}");
        }

        var system =
            "You write the rationale shown next to a recovery suggestion in a finance dashboard. "
            + $"Write at most 2 short sentences in {SupportedLanguages.DisplayName(language)}. "
            + "Use ONLY the facts provided. Do not calculate, estimate or invent any number; if you mention the amount or the days, copy them exactly. "
            + "Do not promise an outcome. Do not mention customer names. Output plain text only.";

        return await TryWriteAsync(system, factText.ToString(), "summarization", 300, fallback, language, cancellationToken);
    }

    public async Task<WrittenText> WriteInsightCommentaryAsync(InsightFacts facts, string language, CancellationToken cancellationToken)
    {
        var failureRate = facts.PaymentCount == 0 ? 0d : (double)facts.FailedCount / facts.PaymentCount;
        var recoveryRate = facts.Lost == 0 ? 0d : (double)(facts.Recovered / facts.Lost);

        var fallback = RationaleTemplates.Insight(facts.Revenue, facts.Failed, failureRate, recoveryRate, facts.PeriodDays, language);

        var builder = new StringBuilder();
        builder.AppendLine($"period: last {facts.PeriodDays} days");
        builder.AppendLine($"revenue collected: {Money(facts.Revenue)} USD (previous period: {Money(facts.PreviousRevenue)} USD)");
        builder.AppendLine($"failed amount: {Money(facts.Failed)} USD (previous period: {Money(facts.PreviousFailed)} USD)");
        builder.AppendLine($"pending amount: {Money(facts.Pending)} USD");
        builder.AppendLine($"payments: {facts.PaymentCount}, failed: {facts.FailedCount}, failure rate: {Percent(failureRate)}%");
        builder.AppendLine($"lost to issues: {Money(facts.Lost)} USD, recovered: {Money(facts.Recovered)} USD, recovery rate: {Percent(recoveryRate)}%");
        builder.AppendLine($"overdue invoices: {facts.OverdueCount} totaling {Money(facts.OverdueAmount)} USD");
        foreach (var reason in facts.TopReasons)
        {
            builder.AppendLine($"top failure reason: {reason.Code}, {reason.Count} payment(s), {Money(reason.Amount)} USD");
        }

        var system =
            "You are a revenue analyst writing a short commentary for a finance dashboard. "
            + $"Write 3 to 4 sentences in {SupportedLanguages.DisplayName(language)}. "
            + "Use ONLY the facts provided and copy numbers exactly as given; never calculate, estimate or invent numbers. "
            + "Point out the most important trend and one practical next step. Output plain text only.";

        return await TryWriteAsync(system, builder.ToString(), "analysis", 600, fallback, language, cancellationToken);
    }

    private async Task<WrittenText> TryWriteAsync(
        string system, string factText, string taskType, int maxTokens, string fallback, string language, CancellationToken cancellationToken)
    {
        try
        {
            var result = await gateway.GenerateAsync(new AiTextRequest(taskType, system, factText, maxTokens), cancellationToken);
            var text = result.Content.Trim();

            if (result.Truncated || text.Length == 0 || text.Length > MaxLength)
            {
                logger.LogInformation("AI text rejected (truncated or empty/too long); using the template.");

                return new WrittenText(fallback, ContentSource.Rule, null);
            }

            if (!NumberGuard.UsesOnlyNumbersFrom(text, factText))
            {
                logger.LogWarning("AI text contained numbers that are not in the provided facts; using the template.");

                return new WrittenText(fallback, ContentSource.Rule, null);
            }

            return new WrittenText(text, ContentSource.Ai, result.RequestId);
        }
        catch (AppException ex)
        {
            logger.LogInformation("AI text unavailable ({Code}); using the template ({Language}).", ex.Code, language);

            return new WrittenText(fallback, ContentSource.Rule, null);
        }
    }

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Percent(double fraction) => (fraction * 100).ToString("0.0", CultureInfo.InvariantCulture);
}
