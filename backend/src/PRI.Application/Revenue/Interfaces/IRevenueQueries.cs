using PRI.Application.Revenue.DTOs;
using PRI.Shared.Models;

namespace PRI.Application.Revenue.Interfaces;

/// <summary>Revenue modülünün salt okunur sorguları. Gerçek uygulaması EF projection ile Infrastructure'dadır.</summary>
public interface IRevenueQueries
{
    Task<PagedResult<PaymentDto>> ListPaymentsAsync(PaymentListQuery query, CancellationToken cancellationToken);

    Task<PaymentDetailDto> GetPaymentAsync(Guid id, CancellationToken cancellationToken);

    Task<RevenueSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<PagedResult<InvoiceDto>> ListInvoicesAsync(InvoiceListQuery query, CancellationToken cancellationToken);

    Task<PagedResult<CustomerDto>> ListCustomersAsync(CustomerListQuery query, CancellationToken cancellationToken);

    Task<RecoverySummaryDto> GetRecoverySummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<PagedResult<IssueDto>> ListIssuesAsync(IssueListQuery query, CancellationToken cancellationToken);

    Task<PagedResult<SuggestionDto>> ListSuggestionsAsync(SuggestionListQuery query, CancellationToken cancellationToken);

    Task<SuggestionDto> GetSuggestionAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Insight için ham USD rakamları (para toplamları veritabanında hesaplanır).</summary>
    Task<InsightFacts> GetInsightFactsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
