using System.Globalization;
using FluentValidation;
using PRI.Application.AiGateway.DTOs;

namespace PRI.Application.AiGateway.Validators;

public sealed class SetBudgetRequestValidator : AbstractValidator<SetBudgetRequest>
{
    public const decimal MaxLimitUsd = 1_000_000m;

    public SetBudgetRequestValidator()
    {
        RuleFor(x => x.Limit).NotNull().WithMessage("limit is required.");

        RuleFor(x => x.Limit.Currency)
            .Must(c => string.Equals(c, "USD", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only USD is supported.")
            .When(x => x.Limit is not null);

        RuleFor(x => x.Limit.Amount)
            .Must(BeAValidLimit)
            .WithMessage($"limit.amount must be a decimal number greater than 0 and at most {MaxLimitUsd:0}, using '.' as the separator (e.g. \"50.00\").")
            .When(x => x.Limit is not null);

        RuleFor(x => x.WarnAtPercent).InclusiveBetween(1, 100).When(x => x.WarnAtPercent is not null);
    }

    private static bool BeAValidLimit(string? amount) =>
        decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
        && value > 0
        && value <= MaxLimitUsd;
}

public sealed class OptimizePromptRequestValidator : AbstractValidator<OptimizePromptRequest>
{
    public OptimizePromptRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotNull().WithMessage("At least one message is required.")
            .Must(messages => messages is { Count: >= 1 and <= 50 })
            .WithMessage("Between 1 and 50 messages are required.");

        RuleForEach(x => x.Messages).SetValidator(new CompletionMessageValidator());

        RuleFor(x => x.Messages)
            .Must(messages => messages is null || messages.Sum(m => m.Content?.Length ?? 0) <= CompletionRequestValidator.MaxTotalCharacters)
            .WithMessage($"The total message length must not exceed {CompletionRequestValidator.MaxTotalCharacters} characters.");
    }
}
