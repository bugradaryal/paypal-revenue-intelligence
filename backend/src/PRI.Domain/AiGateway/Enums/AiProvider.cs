namespace PRI.Domain.AiGateway.Enums;

/// <summary>
/// Desteklenen AI sağlayıcıları. MVP'de yalnızca OpenAI vardır; yeni sağlayıcı = yeni üye + yeni IAiProvider adaptörü.
/// JSON'da "openai" olarak görünür.
/// </summary>
public enum AiProvider
{
    Openai = 1
}
