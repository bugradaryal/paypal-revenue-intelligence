using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

/// <summary>Bir PayPal senkronizasyon çalışmasının kaydı.</summary>
public sealed class SyncRun : BaseEntity
{
    private const int MaxErrorLength = 2000;

    // EF Core için.
    private SyncRun()
    {
    }

    public DataSource Source { get; private set; }

    public SyncRunStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    public DateTime RangeFromUtc { get; private set; }

    public DateTime RangeToUtc { get; private set; }

    public int ImportedPaymentCount { get; private set; }

    public int ImportedInvoiceCount { get; private set; }

    /// <summary>Eksik veya tanınmayan alanlar nedeniyle içe alınmayan kayıt sayısı.</summary>
    public int SkippedCount { get; private set; }

    public string? Error { get; private set; }

    public static SyncRun Start(DateTime rangeFromUtc, DateTime rangeToUtc, DateTime nowUtc)
    {
        if (rangeToUtc <= rangeFromUtc)
        {
            throw new ArgumentException("The sync range end must be after its start.", nameof(rangeToUtc));
        }

        return new SyncRun
        {
            Source = DataSource.Paypal,
            Status = SyncRunStatus.Running,
            StartedAtUtc = nowUtc,
            RangeFromUtc = rangeFromUtc,
            RangeToUtc = rangeToUtc,
            CreatedAtUtc = nowUtc
        };
    }

    public void Complete(int importedPayments, int importedInvoices, int skipped, DateTime nowUtc)
    {
        Status = SyncRunStatus.Succeeded;
        ImportedPaymentCount = importedPayments;
        ImportedInvoiceCount = importedInvoices;
        SkippedCount = skipped;
        FinishedAtUtc = nowUtc;
    }

    public void Fail(string error, DateTime nowUtc)
    {
        Status = SyncRunStatus.Failed;
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        FinishedAtUtc = nowUtc;
    }
}
