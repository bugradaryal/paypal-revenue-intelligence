using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Interfaces;

public interface IPaymentRepository
{
    Task<Dictionary<string, Payment>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken);

    void Add(Payment payment);
}
