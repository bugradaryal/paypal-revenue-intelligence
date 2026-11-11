namespace PRI.Application.AiGateway.Interfaces;

/// <param name="Vector">Sağlayıcının döndürdüğü ham vektör (normalize edilmemiş).</param>
/// <param name="InputTokens">Sağlayıcının bildirdiği gerçek girdi token'ı.</param>
/// <param name="CostUsd">Gerçek token × yapılandırılmış resmi fiyat.</param>
public sealed record EmbeddingResult(float[] Vector, int InputTokens, decimal CostUsd, string ModelId);

public interface IEmbeddingProvider
{
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken);
}
