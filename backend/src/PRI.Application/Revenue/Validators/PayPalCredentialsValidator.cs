using FluentValidation;
using PRI.Application.Revenue.DTOs;

namespace PRI.Application.Revenue.Validators;

public sealed class PayPalCredentialsValidator : AbstractValidator<SavePayPalCredentialsRequest>
{
    public PayPalCredentialsValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().MaximumLength(512);
        RuleFor(x => x.ClientSecret).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.WebhookId).MaximumLength(512);
    }
}
