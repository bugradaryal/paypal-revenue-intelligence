using FluentValidation;
using PRI.Application.Common.DTOs;

namespace PRI.Application.Common.Validators;

public sealed class DateRangeQueryValidator : AbstractValidator<DateRangeQuery>
{
    public DateRangeQueryValidator()
    {
        RuleFor(x => x.From).NotNull();
        RuleFor(x => x.To).NotNull();

        RuleFor(x => x.To)
            .Must((query, to) => query.From is null || to is null || query.From < to)
            .WithMessage("'to' must be after 'from'.");

        RuleFor(x => x.To)
            .Must((query, to) => query.From is null || to is null || to - query.From <= TimeSpan.FromDays(366))
            .WithMessage("The range must not exceed 366 days.");
    }
}
