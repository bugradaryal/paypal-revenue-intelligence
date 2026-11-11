using System.Collections.Concurrent;
using PRI.Application.AiGateway.Interfaces;
using PRI.Application.Revenue.Interfaces;
using PRI.Domain.AiGateway.Enums;

namespace PRI.Api.IntegrationTests;

/// <summary>OpenAI yerine geçer: gerçek maliyet hesabı için sabit token sayıları döndürür. Varsayılan yanıtta rakam yoktur (gerekçe sayı doğrulamasından geçer).</summary>
public sealed class ScriptedAiProvider : IAiProvider
{
    private int _calls;

    public AiProvider Provider => AiProvider.Openai;

    public int Calls => Volatile.Read(ref _calls);

    public Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken)
    {
        var n = Interlocked.Increment(ref _calls);

        return Task.FromResult(new ProviderCompletionResult(
            "A short fake answer without any numbers.", 1000, 0, 500, $"fake-{n}", "stop"));
    }
}

/// <summary>Metne göre önceden verilen vektörü döndürür; verilmeyen metne birbirine dik 16 boyutlu bir vektör atar.</summary>
public sealed class ControllableEmbeddingProvider : IEmbeddingProvider
{
    private int _next;

    public ConcurrentDictionary<string, float[]> Vectors { get; } = new();

    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var vector = Vectors.GetOrAdd(text, _ =>
        {
            var v = new float[16];
            v[Interlocked.Increment(ref _next) % 16] = 1f;

            return v;
        });

        return Task.FromResult(new EmbeddingResult(vector, 10, 0.0000002m, "fake-embedding"));
    }
}

/// <summary>PayPal yerine geçer: gönderilen hatırlatmaları kaydeder, istenirse hata verir.</summary>
public sealed class RecordingPayPalClient : IPayPalClient
{
    public ConcurrentQueue<string> Reminders { get; } = new();

    public bool IsConfigured => true;

    public string Environment => "sandbox";

    public Task TestConnectionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Doluysa bir sonraki hatırlatma bu hatayla başarısız olur.</summary>
    public Exception? ReminderError { get; set; }

    public Task SendInvoiceReminderAsync(string invoiceId, CancellationToken cancellationToken)
    {
        if (ReminderError is not null)
        {
            throw ReminderError;
        }

        Reminders.Enqueue(invoiceId);

        return Task.CompletedTask;
    }

    public Task<PayPalTransactionPage> SearchTransactionsAsync(DateTime fromUtc, DateTime toUtc, int page, CancellationToken cancellationToken) =>
        throw new NotSupportedException("PayPal sync is not exercised by the integration tests.");

    public Task<PayPalInvoicePage> ListInvoicesAsync(int page, CancellationToken cancellationToken) =>
        throw new NotSupportedException("PayPal sync is not exercised by the integration tests.");
}
