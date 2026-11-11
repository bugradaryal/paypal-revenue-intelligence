using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PRI.Shared.Constants;
using PRI.Shared.Exceptions;

namespace PRI.Api.Middleware;

/// <summary>Yakalanan tüm istisnaları API_CONTRACT.md'deki ProblemDetails formatına çevirir.</summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // İstemci bağlantıyı kapattıysa yazılacak bir yanıt yoktur.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title, code) = exception switch
        {
            RequestValidationException => (StatusCodes.Status400BadRequest, "Validation failed", ErrorCodes.ValidationFailed),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", ErrorCodes.Unauthorized),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ErrorCodes.NotFound),
            ConflictException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Code),
            BusinessRuleException ex => (StatusCodes.Status422UnprocessableEntity, "Business rule violation", ex.Code),
            ExternalServiceException ex => (StatusCodes.Status502BadGateway, "External service error", ex.Code),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", ErrorCodes.InternalError)
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request rejected: {Code} - {Message}", code, exception.Message);
        }

        ProblemDetails problem = exception is RequestValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        // Beklenmeyen hatalarda iç detay istemciye sızdırılmaz.
        problem.Detail = exception is AppException ? exception.Message : "An unexpected error occurred.";
        problem.Extensions["code"] = code;

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
