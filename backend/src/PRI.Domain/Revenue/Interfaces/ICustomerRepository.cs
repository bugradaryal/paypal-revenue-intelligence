using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Interfaces;

public interface ICustomerRepository
{
    Task<Dictionary<string, Customer>> GetByExternalIdsAsync(
        DataSource source, IReadOnlyCollection<string> externalIds, CancellationToken cancellationToken);

    void Add(Customer customer);
}
