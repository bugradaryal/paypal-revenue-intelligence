using PRI.Application.Revenue.Interfaces;

namespace PRI.Api.BackgroundJobs;

/// <summary>Kuyruktaki onaylı önerileri sırayla, her biri kendi DI kapsamında çalıştırır.</summary>
public sealed class RecoveryActionWorker(
    RecoveryActionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<RecoveryActionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var actionId in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IRecoveryActionExecutor>();
                    await executor.ExecuteAsync(actionId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Bir önerinin hatası kuyruğu durdurmamalı; executor kendi hatalarını zaten Failed olarak kaydeder.
                    logger.LogError(ex, "Recovery action {ActionId} could not be executed.", actionId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor.
        }
    }
}
