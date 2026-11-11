using PRI.Shared.Constants;

namespace PRI.Shared.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string resource, object key)
        : base(ErrorCodes.NotFound, $"{resource} '{key}' was not found.")
    {
    }
}
