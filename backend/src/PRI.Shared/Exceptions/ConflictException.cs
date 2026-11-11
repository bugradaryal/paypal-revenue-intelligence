using PRI.Shared.Constants;

namespace PRI.Shared.Exceptions;

public sealed class ConflictException : AppException
{
    public ConflictException(string message, string code = ErrorCodes.Conflict) : base(code, message)
    {
    }
}
