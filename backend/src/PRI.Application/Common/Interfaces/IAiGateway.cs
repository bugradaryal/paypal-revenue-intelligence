namespace PRI.Application.Common.Interfaces;

/// <param name="TaskType">Gateway yönlendirme görev tipi (ör. "analysis", "summarization").</param>
/// <param name="MaxOutputTokens">Gizli akıl yürütme token'ları da bu sınıra sayılır; yeterince geniş tutulmalıdır.</param>
public sealed record AiTextRequest(string TaskType, string SystemPrompt, string UserPrompt, int MaxOutputTokens);

/// <param name="Truncated">Yanıt token sınırında kesildiyse true; çağıran tam sayılmamalıdır.</param>
/// <param name="RequestId">Gateway'in kayıt kimliği (kullanım kaydı veya cache arama kaydı).</param>
public sealed record AiTextResult(string Content, bool Truncated, Guid RequestId);

/// <summary>
/// Diğer modüllerin yapay zekâyı kullanma sözleşmesi. Revenue gibi modüller AI Gateway'in iç tiplerini bilmez, yalnızca
/// bunu çağırır; modeller, yönlendirme, cache, bütçe ve maliyet takibi gateway'in sorumluluğundadır.
/// Hata durumunda AppException (ör. budget_exceeded, openai_not_configured, provider_unavailable) fırlatır.
/// </summary>
public interface IAiGateway
{
    Task<AiTextResult> GenerateAsync(AiTextRequest request, CancellationToken cancellationToken);
}
