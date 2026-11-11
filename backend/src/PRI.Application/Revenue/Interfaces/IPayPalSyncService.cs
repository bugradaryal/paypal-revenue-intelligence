using PRI.Application.Revenue.DTOs;

namespace PRI.Application.Revenue.Interfaces;

public interface IPayPalSyncService
{
    /// <summary>Son <paramref name="days"/> günün fatura ve işlemlerini PayPal Sandbox'tan içe alır.</summary>
    Task<SyncRunDto> SyncAsync(int days, CancellationToken cancellationToken);

    Task<SyncRunDto> GetLatestAsync(CancellationToken cancellationToken);

    Task<PayPalConnectionDto> GetConnectionAsync(CancellationToken cancellationToken);

    Task<PayPalTestConnectionDto> TestConnectionAsync(CancellationToken cancellationToken);
}
