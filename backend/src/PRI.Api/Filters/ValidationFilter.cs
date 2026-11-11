using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using PRI.Shared.Exceptions;

namespace PRI.Api.Filters;

/// <summary>
/// Action parametreleri için kayıtlı FluentValidation validator'larını çalıştırır.
/// Hata varsa RequestValidationException fırlatır; GlobalExceptionHandler bunu ProblemDetails'e çevirir.
/// </summary>
public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => ToCamelCase(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

                throw new RequestValidationException(errors);
            }
        }

        await next();
    }

    // "Address.Street" -> "address.street" (JSON property isimleriyle uyumlu olsun).
    private static string ToCamelCase(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(part =>
            part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
}
