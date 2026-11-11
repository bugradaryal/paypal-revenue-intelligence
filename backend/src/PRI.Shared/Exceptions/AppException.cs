namespace PRI.Shared.Exceptions;

/// <summary>Beklenen (istemciye bildirilecek) hataların ortak tabanı. API katmanında ProblemDetails'e çevrilir.</summary>
public abstract class AppException : Exception
{
    protected AppException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
