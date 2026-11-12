using PRI.Application.Revenue.DTOs;

namespace PRI.Application.Revenue.Interfaces;

public interface ICashFlowOutlookQueries
{
    Task<CashFlowOutlookDto> GetOutlookAsync(CancellationToken cancellationToken);
}
