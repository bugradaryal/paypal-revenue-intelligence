using FluentValidation;
using PRI.Application.Common.DTOs;
using PRI.Domain.Common;

namespace PRI.Application.Common.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        RuleFor(x => x.Role)
            .Must(role => role is UserRole.Admin or UserRole.Member)
            .WithMessage("Role must be admin or member.");
    }
}
