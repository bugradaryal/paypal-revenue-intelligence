namespace PRI.Domain.AiGateway.Enums;

/// <summary>Bir cache aramasının sonucu. Hit: sağlayıcı çağrılmadan kayıtlı yanıt döndü.</summary>
public enum CacheOutcome
{
    Miss = 1,
    ExactHit = 2,
    SemanticHit = 3
}
