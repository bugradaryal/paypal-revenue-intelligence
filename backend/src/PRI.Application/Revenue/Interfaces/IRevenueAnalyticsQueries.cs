using PRI.Application.Revenue.DTOs;

namespace PRI.Application.Revenue.Interfaces;

public interface IRevenueAnalyticsQueries
{
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
