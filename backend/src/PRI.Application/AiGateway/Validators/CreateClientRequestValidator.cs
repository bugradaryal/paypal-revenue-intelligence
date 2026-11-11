using FluentValidation;
using PRI.Application.AiGateway.DTOs;

namespace PRI.Application.AiGateway.Validators;

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
