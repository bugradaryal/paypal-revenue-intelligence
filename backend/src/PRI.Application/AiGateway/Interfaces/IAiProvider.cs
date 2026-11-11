using PRI.Domain.AiGateway.Enums;

namespace PRI.Application.AiGateway.Interfaces;

public sealed record ProviderMessage(string Role, string Content);

public sealed record ProviderCompletionRequest(
    string ModelId,
    IReadOnlyList<ProviderMessage> Messages,
    int? MaxOutputTokens,
    double? Temperature,
    string? ReasoningEffort);

/// <param name="InputTokens">Toplam girdi token'ı (önbellekten okunanlar dahil).</param>
/// <param name="CachedInputTokens">Girdinin önbellekten okunan kısmı.</param>
/// <param name="OutputTokens">Çıktı token'ı (gizli akıl yürütme token'ları dahil, sağlayıcı faturalandırdığı gibi).</param>
public sealed record ProviderCompletionResult(
    string Content,
    int InputTokens,
    int CachedInputTokens,
    int OutputTokens,
    string? ProviderRequestId,
    string? FinishReason);

/// <summary>
/// Sağlayıcı adaptörü: her AI sağlayıcısı bu arayüzü uygular. Yeni sağlayıcı eklemek, AiProvider'a bir üye ve
/// Infrastructure'da yeni bir adaptör eklemekten ibarettir; gateway'in geri kalanı değişmez.
/// </summary>
public interface IAiProvider
{
    AiProvider Provider { get; }

    Task<ProviderCompletionResult> CompleteAsync(ProviderCompletionRequest request, CancellationToken cancellationToken);
}

public interface IAiProviderResolver
{
    IAiProvider Get(AiProvider provider);
}
