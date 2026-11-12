namespace PRI.Domain.AiGateway.Enums;

/// <summary>
/// AI sağlayıcı kayıtları. Anthropic enum değeri yalnızca eski veritabanı kayıtlarını okuyup devre dışı bırakabilmek
/// için tutulur; bu uygulama sürümünde yalnızca OpenAI sağlayıcısı desteklenir.
/// </summary>
public enum AiProvider
{
    Openai = 1,
    Anthropic = 2
}
