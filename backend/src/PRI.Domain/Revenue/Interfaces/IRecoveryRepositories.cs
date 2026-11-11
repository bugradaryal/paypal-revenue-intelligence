using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Interfaces;

public readonly record struct IssueKey(IssueType Type, Guid? PaymentId, Guid? InvoiceId);

public interface IRevenueIssueRepository
{
    /// <summary>Açık kayıplar (takip edilen; durum güncellenecek).</summary>
    Task<IReadOnlyList<RevenueIssue>> ListOpenAsync(CancellationToken cancellationToken);

    /// <summary>Durumu ne olursa olsun var olan tüm kayıpların anahtarları (yinelenen tespiti önler).</summary>
    Task<HashSet<IssueKey>> ListKeysAsync(CancellationToken cancellationToken);

    void Add(RevenueIssue issue);
}

public interface IRecoveryActionRepository
{
    Task<RecoveryAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Yeniden öneri üretilmemesi gereken kayıplar: Proposed, Approved, Executed veya Rejected önerisi olanlar.</summary>
    Task<HashSet<Guid>> ListIssueIdsWithBlockingActionsAsync(CancellationToken cancellationToken);

    void Add(RecoveryAction action);
}

public interface IAiInsightRepository
{
    void Add(AiInsight insight);

    /// <summary>En yeniden eskiye sayfalı liste ve toplam kayıt sayısı.</summary>
    Task<(IReadOnlyList<AiInsight> Items, int Total)> ListLatestAsync(int page, int pageSize, CancellationToken cancellationToken);
}

/// <summary>Tespit ve çözüm kurallarının ihtiyaç duyduğu salt okunur olgular.</summary>
public interface IRevenueFactsReader
{
    Task<IReadOnlyList<PaymentFact>> ListPaymentsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentFact>> ListPaymentsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Gönderilmiş, kısmen ödenmiş veya ödemesi bekleyen faturalar.</summary>
    Task<IReadOnlyList<InvoiceFact>> ListUnsettledInvoicesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceFact>> ListInvoicesByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
