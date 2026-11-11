using PRI.Shared.Constants;

namespace PRI.Shared.Exceptions;

public sealed class RequestValidationException : AppException
{
    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(ErrorCodes.ValidationFailed, "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
