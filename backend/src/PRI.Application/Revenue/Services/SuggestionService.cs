using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// Kurtarma önerilerini üretir ve kullanıcı kararını işler. Öneri türü ve tutar kurallarla belirlenir; yapay zekâ yalnızca
/// gerekçe metnini yazar. Hiçbir adım kullanıcı onayı olmadan çalışmaz.
/// </summary>
public sealed class SuggestionService(
    IRevenueIssueSync issueSync,
    IRevenueIssueRepository issues,
    IRecoveryActionRepository actions,
    IRevenueFactsReader facts,
    IRationaleWriter rationaleWriter,
    IRecoveryActionQueue queue,
    IRevenueQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ISuggestionService
{
    /// <summary>Tek çalıştırmada üretilecek en fazla öneri (AI maliyetini ve gecikmeyi sınırlar).</summary>
    public const int MaxPerRun = 10;

    public async Task<GenerateSuggestionsResultDto> GenerateAsync(string language, CancellationToken cancellationToken)
    {
        await issueSync.RefreshAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var open = await issues.ListOpenAsync(cancellationToken);
        var blocked = await actions.ListIssueIdsWithBlockingActionsAsync(cancellationToken);

        var candidates = open
            .Where(i => !blocked.Contains(i.Id))
            .OrderByDescending(i => i.Amount)
            .ThenBy(i => i.ProblemSinceUtc)
            .Take(MaxPerRun)
            .ToList();

        if (candidates.Count == 0)
        {
            return new GenerateSuggestionsResultDto(0);
        }

        var invoiceIds = candidates.Where(i => i.InvoiceId is not null).Select(i => i.InvoiceId!.Value).Distinct().ToList();
        var invoices = invoiceIds.Count == 0
            ? new Dictionary<Guid, InvoiceFact>()
            : (await facts.ListInvoicesByIdsAsync(invoiceIds, cancellationToken)).ToDictionary(i => i.Id);

        // Sırayla: gateway çağrıları aynı DbContext'i kullanır, paralel çalıştırılamaz.
        foreach (var issue in candidates)
        {
            var invoice = issue.InvoiceId is { } invoiceId && invoices.TryGetValue(invoiceId, out var found) ? found : null;
            var (type, executable) = RecoveryPlanner.Plan(issue, invoice);

            var written = await rationaleWriter.WriteRationaleAsync(
                new RationaleFacts(type, issue.Type, issue.Amount, issue.Currency, issue.FailureCode, issue.DaysOutstanding(now)),
                language,
                cancellationToken);

            actions.Add(RecoveryAction.Propose(
                issue, type, executable, written.Text, language, written.Source, written.GatewayRequestId, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new GenerateSuggestionsResultDto(candidates.Count);
    }

    public async Task<SuggestionDto> DecideAsync(Guid id, string decision, Guid userId, CancellationToken cancellationToken)
    {
        var action = await actions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Suggestion", id);

        if (action.IsDecided)
        {
            throw new ConflictException($"The suggestion is already {action.Status.ToString().ToLowerInvariant()}.", "suggestion_already_decided");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var approve = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase);

        if (approve)
        {
            action.Approve(userId, now);
        }
        else
        {
            action.Reject(userId, now);
        }

        // Eşzamanlı ikinci karar (çift tıklama) burada concurrent_update/409 ile reddedilir; adım iki kez kuyruğa girmez.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (approve && action.Executable)
        {
            await queue.EnqueueAsync(action.Id);
        }

        return await queries.GetSuggestionAsync(id, cancellationToken);
    }
}
