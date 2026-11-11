namespace PRI.Shared.Exceptions;

/// <summary>İş kuralı ihlali (HTTP 422). Her ihlal kendi makine okunur koduyla gelir.</summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string code, string message) : base(code, message)
    {
    }
}
