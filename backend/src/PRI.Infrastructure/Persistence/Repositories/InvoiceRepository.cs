using Microsoft.EntityFrameworkCore;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;
using PRI.Domain.Revenue.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    public async Task<Dictionary<string, Invoice>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken)
    {
        var ids = externalIds.ToList();

        return await db.Invoices
            .Where(i => i.DataSource == source && ids.Contains(i.ExternalId))
            .ToDictionaryAsync(i => i.ExternalId, cancellationToken);
    }

    public async Task<Dictionary<string, Invoice>> GetByInvoiceNumbersAsync(
        DataSource source, IReadOnlyCollection<string> invoiceNumbers, CancellationToken cancellationToken)
    {
        var numbers = invoiceNumbers.ToList();

        var found = await db.Invoices
            .Where(i => i.DataSource == source && i.InvoiceNumber != null && numbers.Contains(i.InvoiceNumber))
            .ToListAsync(cancellationToken);

        // Fatura numarası benzersiz olmak zorunda değildir; aynı numaradan birden fazla varsa ilki seçilir.
        return found
            .GroupBy(i => i.InvoiceNumber!)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);
}
