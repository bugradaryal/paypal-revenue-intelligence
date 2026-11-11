namespace PRI.Shared.Constants;

/// <summary>Makine okunur hata kodları. API_CONTRACT.md bölüm 3 ile birebir aynı tutulmalıdır.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string BadRequest = "bad_request";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string MethodNotAllowed = "method_not_allowed";
    public const string Conflict = "conflict";
    public const string BusinessRuleViolation = "business_rule_violation";
    public const string BadGateway = "bad_gateway";
    public const string InternalError = "internal_error";

    public static string FromStatusCode(int? statusCode) => statusCode switch
    {
        400 => BadRequest,
        401 => Unauthorized,
        403 => Forbidden,
        404 => NotFound,
        405 => MethodNotAllowed,
        409 => Conflict,
        422 => BusinessRuleViolation,
        502 => BadGateway,
        >= 500 => InternalError,
        _ => "error"
    };
}
