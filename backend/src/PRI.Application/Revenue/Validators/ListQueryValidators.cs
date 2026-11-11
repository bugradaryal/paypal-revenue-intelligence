using FluentValidation;
using PRI.Application.Common;
using PRI.Application.Revenue.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Validators;

public sealed class PaymentListQueryValidator : AbstractValidator<PaymentListQuery>
{
    private static readonly string[] SortFields = ["createdAt", "amount"];

    public PaymentListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleForEach(x => x.Status)
            .Must(value => SnakeCaseEnum.TryParse<PaymentStatus>(value, out _))
            .WithMessage("Unknown payment status '{PropertyValue}'.");
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: createdAt, amount.");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
        RuleFor(x => x.To)
            .Must((query, to) => query.From is null || to is null || query.From < to)
            .WithMessage("'to' must be after 'from'.");
    }
}

public sealed class InvoiceListQueryValidator : AbstractValidator<InvoiceListQuery>
{
    private static readonly string[] SortFields = ["issuedAt", "dueDate", "amount"];

    public InvoiceListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleForEach(x => x.Status)
            .Must(value => SnakeCaseEnum.TryParse<InvoiceStatus>(value, out _))
            .WithMessage("Unknown invoice status '{PropertyValue}'.");
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: issuedAt, dueDate, amount.");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
        RuleFor(x => x.To)
            .Must((query, to) => query.From is null || to is null || query.From < to)
            .WithMessage("'to' must be after 'from'.");
    }
}

public sealed class CustomerListQueryValidator : AbstractValidator<CustomerListQuery>
{
    private static readonly string[] SortFields = ["createdAt", "name"];

    public CustomerListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: createdAt, name.");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
    }
}

public sealed class SyncRequestValidator : AbstractValidator<SyncRequest>
{
    public SyncRequestValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 90);
    }
}
