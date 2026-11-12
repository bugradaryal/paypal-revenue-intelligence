using Microsoft.Extensions.Logging;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Revenue.Services;

/// <summary>
/// PayPal Sandbox'tan fatura ve işlemleri içe alır (upsert). Para ve durum bilgisi yalnızca PayPal'ın verdiği veridir;
/// tanınmayan kayıtlar uydurulmaz, atlanır ve sayılır.
/// </summary>
public sealed class PayPalSyncService(
    IPayPalClient payPal,
    ICustomerRepository customers,
    IPaymentRepository payments,
    IInvoiceRepository invoices,
    ISyncRunRepository syncRuns,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PayPalSyncService> logger) : IPayPalSyncService
{
    private const int MaxWindowDays = 31;   // PayPal Transaction Search: tek istekte en fazla 31 gün
    private const int MaxPages = 200;       // sonsuz döngüye karşı emniyet
    private static readonly TimeSpan StaleRunAfter = TimeSpan.FromMinutes(10);

    public async Task<SyncRunDto> SyncAsync(int days, CancellationToken cancellationToken)
    {
        if (!payPal.IsConfigured)
        {
            throw new BusinessRuleException(
                "paypal_not_configured",
                "PayPal credentials have not been configured.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (await syncRuns.HasRunningSinceAsync(now - StaleRunAfter, cancellationToken))
        {
            throw new ConflictException("A PayPal sync is already running.", "sync_in_progress");
        }

        var from = now.AddDays(-days);
        var run = SyncRun.Start(from, now, now);
        syncRuns.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var context = new SyncContext();

            await ImportInvoicesAsync(context, now, cancellationToken);
            await ImportTransactionsAsync(context, from, now, now, cancellationToken);
            await ApplyRefundsAsync(context, now, cancellationToken);

            run.Complete(
                context.ImportedPayments, context.ImportedInvoices, context.Skipped,
                timeProvider.GetUtcNow().UtcDateTime, context.InvoiceImportSkipped);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex switch
            {
                AppException => ex.Message,
                OperationCanceledException => "The sync was cancelled.",
                _ => "Unexpected error during sync (see server logs)."
            };

            if (ex is AppException)
            {
                logger.LogWarning("PayPal sync failed: {Message}", ex.Message);
            }
            else
            {
                logger.LogError(ex, "PayPal sync failed unexpectedly.");
            }

            run.Fail(message, timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return ToDto(run);
    }

    public async Task<SyncRunDto> GetLatestAsync(CancellationToken cancellationToken)
    {
        var run = await syncRuns.GetLatestAsync(cancellationToken)
            ?? throw new NotFoundException("SyncRun", "latest");

        return ToDto(run);
    }

    public async Task<PayPalConnectionDto> GetConnectionAsync(CancellationToken cancellationToken)
    {
        var test = payPal.IsConfigured
            ? await TestConnectionAsync(cancellationToken)
            : new PayPalTestConnectionDto(false, payPal.Environment, timeProvider.GetUtcNow().UtcDateTime, null);
        var latest = await syncRuns.GetLatestAsync(cancellationToken);
        var latestSuccessful = await syncRuns.GetLatestSuccessfulAsync(cancellationToken);

        return new PayPalConnectionDto(
            payPal.IsConfigured,
            payPal.HasWebhookId,
            test.Connected,
            payPal.Environment,
            test.Connected ? "available" : payPal.IsConfigured ? "denied" : "not_configured",
            latestSuccessful?.FinishedAtUtc,
            latest is null ? null : ToDto(latest),
            test.Error);
    }

    public async Task<PayPalTestConnectionDto> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var testedAt = timeProvider.GetUtcNow().UtcDateTime;
        if (!payPal.IsConfigured)
        {
            return new PayPalTestConnectionDto(false, payPal.Environment, testedAt, null);
        }

        try
        {
            await payPal.TestConnectionAsync(cancellationToken);
            return new PayPalTestConnectionDto(true, payPal.Environment, testedAt, null);
        }
        catch (AppException ex)
        {
            logger.LogWarning("PayPal connection test failed: {Code}", ex.Code);
            return new PayPalTestConnectionDto(false, payPal.Environment, testedAt, ex.Message);
        }
    }

    // ---------- Faturalar ----------

    private async Task ImportInvoicesAsync(SyncContext context, DateTime now, CancellationToken cancellationToken)
    {
        var page = 1;

        while (true)
        {
            PayPalInvoicePage result;
            try
            {
                result = await payPal.ListInvoicesAsync(page, cancellationToken);
            }
            catch (ExternalServiceException ex) when (ex.Code == "paypal_invoicing_forbidden")
            {
                context.InvoiceImportSkipped = true;
                logger.LogWarning("PayPal invoice import skipped because the Sandbox app has no Invoicing access.");
                break;
            }

            context.Skipped += result.SkippedCount;

            await UpsertInvoicesAsync(context, result.Items, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (page >= result.TotalPages || page >= MaxPages)
            {
                break;
            }

            page++;
        }
    }

    private async Task UpsertInvoicesAsync(
        SyncContext context, IReadOnlyList<PayPalInvoice> items, DateTime now, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var existing = await invoices.GetByExternalIdsAsync(
            DataSource.Paypal, items.Select(i => i.InvoiceId).ToList(), cancellationToken);

        await LoadCustomersAsync(
            context, items.Select(i => CustomerKey(i.RecipientEmail, null)).OfType<string>(), cancellationToken);

        foreach (var item in items)
        {
            if (!PayPalStatusMapper.TryMapInvoiceStatus(item.StatusCode, out var status))
            {
                context.Skipped++;
                continue;
            }

            Guid? customerId = null;
            var key = CustomerKey(item.RecipientEmail, null);
            if (key is not null)
            {
                customerId = EnsureCustomer(context, key, item.RecipientName, item.RecipientEmail, now).Id;
            }

            var updatedAt = item.UpdatedAtUtc ?? item.IssuedAtUtc ?? now;

            if (existing.TryGetValue(item.InvoiceId, out var invoice))
            {
                invoice.ApplyProviderUpdate(customerId, item.Amount, status, item.StatusCode, item.DueDate, updatedAt, now);
            }
            else
            {
                invoice = Invoice.Create(
                    DataSource.Paypal, item.InvoiceId, item.InvoiceNumber, customerId, item.Amount, item.Currency,
                    status, item.StatusCode, item.DueDate, item.IssuedAtUtc, updatedAt, now);
                invoices.Add(invoice);
            }

            if (invoice.InvoiceNumber is not null)
            {
                context.InvoicesByNumber[invoice.InvoiceNumber] = invoice;
            }

            context.ImportedInvoices++;
        }
    }

    // ---------- İşlemler ----------

    private async Task ImportTransactionsAsync(
        SyncContext context, DateTime from, DateTime to, DateTime now, CancellationToken cancellationToken)
    {
        var windowStart = from;

        while (windowStart < to)
        {
            var windowEnd = windowStart.AddDays(MaxWindowDays) < to ? windowStart.AddDays(MaxWindowDays) : to;
            var page = 1;

            while (true)
            {
                var result = await payPal.SearchTransactionsAsync(windowStart, windowEnd, page, cancellationToken);
                context.Skipped += result.SkippedCount;

                await ProcessTransactionsAsync(context, result.Items, now, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                if (page >= result.TotalPages || page >= MaxPages)
                {
                    break;
                }

                page++;
            }

            windowStart = windowEnd;
        }
    }

    private async Task ProcessTransactionsAsync(
        SyncContext context, IReadOnlyList<PayPalTransaction> items, DateTime now, CancellationToken cancellationToken)
    {
        // Negatif tutarlı kayıtlar ödeme değildir; iade ise orijinal ödemeye bağlıdır (en sonda uygulanır).
        var candidates = new List<(PayPalTransaction Item, PaymentStatus Status)>();

        foreach (var item in items)
        {
            if (item.Amount < 0)
            {
                if (item.ReferenceIdType == "TXN" && item.ReferenceTransactionId is not null)
                {
                    context.PendingRefunds[item.ReferenceTransactionId] = item.UpdatedAtUtc;
                }

                continue;
            }

            if (item.Amount == 0)
            {
                continue;
            }

            if (!PayPalStatusMapper.TryMapTransactionStatus(item.StatusCode, out var status))
            {
                context.Skipped++;
                continue;
            }

            candidates.Add((item, status));
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var existingPayments = await payments.GetByExternalIdsAsync(
            DataSource.Paypal, candidates.Select(c => c.Item.TransactionId).ToList(), cancellationToken);

        await LoadCustomersAsync(
            context,
            candidates.Select(c => CustomerKey(c.Item.PayerEmail, c.Item.PayerAccountId)).OfType<string>(),
            cancellationToken);

        var missingNumbers = candidates
            .Select(c => c.Item.InvoiceNumber)
            .OfType<string>()
            .Where(number => !context.InvoicesByNumber.ContainsKey(number))
            .Distinct()
            .ToList();

        if (missingNumbers.Count > 0)
        {
            foreach (var (number, invoice) in await invoices.GetByInvoiceNumbersAsync(DataSource.Paypal, missingNumbers, cancellationToken))
            {
                context.InvoicesByNumber[number] = invoice;
            }
        }

        foreach (var (item, status) in candidates)
        {
            var key = CustomerKey(item.PayerEmail, item.PayerAccountId);
            if (key is null)
            {
                // Ödemenin kime ait olduğu bilinmiyor (ör. reddedilen işlemlerde payer bilgisi olmayabilir).
                context.Skipped++;
                continue;
            }

            var customer = EnsureCustomer(context, key, item.PayerName, item.PayerEmail, now);

            Invoice? invoice = null;
            if (item.InvoiceNumber is not null)
            {
                context.InvoicesByNumber.TryGetValue(item.InvoiceNumber, out invoice);
            }

            if (existingPayments.TryGetValue(item.TransactionId, out var payment))
            {
                var changed = payment.ApplyProviderUpdate(status, null, item.UpdatedAtUtc, now);

                if (invoice is not null && payment.InvoiceId is null)
                {
                    payment.LinkInvoice(invoice.Id);
                    changed = true;
                }

                if (changed)
                {
                    context.ImportedPayments++;
                }
            }
            else
            {
                payment = Payment.Create(
                    customer.Id, DataSource.Paypal, item.TransactionId, item.Amount, item.Currency, status, null,
                    item.InitiatedAtUtc, item.UpdatedAtUtc, invoice?.Id, now);
                payments.Add(payment);
                context.ImportedPayments++;
            }
        }
    }

    private async Task ApplyRefundsAsync(SyncContext context, DateTime now, CancellationToken cancellationToken)
    {
        if (context.PendingRefunds.Count == 0)
        {
            return;
        }

        var originals = await payments.GetByExternalIdsAsync(
            DataSource.Paypal, context.PendingRefunds.Keys.ToList(), cancellationToken);

        foreach (var (transactionId, refundedAt) in context.PendingRefunds)
        {
            // Orijinal ödeme içe alınan aralığın dışındaysa bulunamaz; o durumda dokunulmaz.
            if (originals.TryGetValue(transactionId, out var payment)
                && payment.ApplyProviderUpdate(PaymentStatus.Refunded, null, refundedAt, now))
            {
                context.ImportedPayments++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- Müşteriler ----------

    // Anahtar öncelikle e-posta (fatura alıcısı ile ödeme sahibi aynı müşteriyle eşleşsin), yoksa PayPal payer id.
    private static string? CustomerKey(string? email, string? payerAccountId)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return $"email:{email.Trim().ToLowerInvariant()}";
        }

        return string.IsNullOrWhiteSpace(payerAccountId) ? null : $"payer:{payerAccountId.Trim()}";
    }

    private async Task LoadCustomersAsync(SyncContext context, IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        var missing = keys.Where(key => !context.Customers.ContainsKey(key)).Distinct().ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var (key, customer) in await customers.GetByExternalIdsAsync(DataSource.Paypal, missing, cancellationToken))
        {
            context.Customers[key] = customer;
        }
    }

    private Customer EnsureCustomer(SyncContext context, string key, string? name, string? email, DateTime now)
    {
        var displayName = !string.IsNullOrWhiteSpace(name) ? name : (email ?? key);

        if (context.Customers.TryGetValue(key, out var customer))
        {
            customer.UpdateProfile(displayName, email);
            return customer;
        }

        customer = Customer.Create(DataSource.Paypal, key, displayName, email, now);
        customers.Add(customer);
        context.Customers[key] = customer;

        return customer;
    }

    private static SyncRunDto ToDto(SyncRun run) =>
        new(run.Id, run.Status, run.StartedAtUtc, run.FinishedAtUtc, run.RangeFromUtc, run.RangeToUtc,
            run.ImportedPaymentCount, run.ImportedInvoiceCount, run.SkippedCount, run.InvoiceImportSkipped, run.Error);

    /// <summary>Bir senkron çalışması boyunca sayfalar arasında paylaşılan önbellek ve sayaçlar.</summary>
    private sealed class SyncContext
    {
        public Dictionary<string, Customer> Customers { get; } = [];

        public Dictionary<string, Invoice> InvoicesByNumber { get; } = [];

        /// <summary>İade edilen orijinal işlem id'si ve iade zamanı.</summary>
        public Dictionary<string, DateTime> PendingRefunds { get; } = [];

        public int ImportedPayments { get; set; }

        public int ImportedInvoices { get; set; }

        public int Skipped { get; set; }

        public bool InvoiceImportSkipped { get; set; }
    }
}
