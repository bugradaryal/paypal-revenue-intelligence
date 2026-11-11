using System.Threading.Channels;
using PRI.Application.Revenue.Interfaces;

namespace PRI.Api.BackgroundJobs;

/// <summary>
/// Onaylanan, çalıştırılabilir önerilerin süreç içi kuyruğu. MVP tek sunucuda çalışır; kuyruk bellekte olduğu için süreç
/// yeniden başlarsa henüz çalıştırılmamış onaylı öneriler "approved" durumunda kalır (otomatik yeniden çalıştırılmaz,
/// böylece müşteriye çift hatırlatma gitmez).
/// </summary>
public sealed class RecoveryActionQueue : IRecoveryActionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid actionId) => _channel.Writer.WriteAsync(actionId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
