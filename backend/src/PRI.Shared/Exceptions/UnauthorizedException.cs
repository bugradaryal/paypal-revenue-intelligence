using PRI.Shared.Constants;

namespace PRI.Shared.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(ErrorCodes.Unauthorized, message)
    {
    }
}
