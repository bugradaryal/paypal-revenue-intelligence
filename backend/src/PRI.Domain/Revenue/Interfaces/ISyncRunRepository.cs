using PRI.Domain.Revenue.Entities;

namespace PRI.Domain.Revenue.Interfaces;

public interface ISyncRunRepository
{
    Task<SyncRun?> GetLatestAsync(CancellationToken cancellationToken);

    Task<SyncRun?> GetLatestSuccessfulAsync(CancellationToken cancellationToken);

    /// <summary>Verilen andan sonra başlamış ve hâlâ "Running" durumunda olan bir çalışma var mı?</summary>
    Task<bool> HasRunningSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);

    void Add(SyncRun run);
}
