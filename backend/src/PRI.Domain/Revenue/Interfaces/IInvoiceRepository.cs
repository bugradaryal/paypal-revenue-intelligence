using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Interfaces;

public interface IInvoiceRepository
{
    Task<Dictionary<string, Invoice>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken);

    Task<Dictionary<string, Invoice>> GetByInvoiceNumbersAsync(
        DataSource source, IReadOnlyCollection<string> invoiceNumbers, CancellationToken cancellationToken);

    void Add(Invoice invoice);
}
