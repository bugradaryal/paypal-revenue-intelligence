namespace PRI.Shared.Exceptions;

/// <summary>Dış servis (PayPal, AI sağlayıcısı) erişilemedi veya hata döndü (HTTP 502).</summary>
public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string code, string message) : base(code, message)
    {
    }
}
