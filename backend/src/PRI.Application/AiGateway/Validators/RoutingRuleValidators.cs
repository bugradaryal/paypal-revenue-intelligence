using System.Text.RegularExpressions;
using FluentValidation;
using PRI.Application.AiGateway.DTOs;

namespace PRI.Application.AiGateway.Validators;

public sealed partial class SetRoutingRuleRequestValidator : AbstractValidator<SetRoutingRuleRequest>
{
    public const int MaxFallbacks = 5;

    public SetRoutingRuleRequestValidator()
    {
        RuleFor(x => x.TaskType)
            .NotEmpty()
            .MaximumLength(50)
            .Must(value => value is not null && TaskTypePattern().IsMatch(value))
            .WithMessage("taskType must be lowercase letters, digits, '_' or '-' (e.g. \"analysis\").");

        RuleFor(x => x.PrimaryModelId).NotEmpty().MaximumLength(100);

        RuleFor(x => x.FallbackModelIds)
            .Must(list => list is null || list.Count <= MaxFallbacks)
            .WithMessage($"At most {MaxFallbacks} fallback models are allowed.");

        RuleForEach(x => x.FallbackModelIds).NotEmpty().MaximumLength(100);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]*$")]
    private static partial Regex TaskTypePattern();
}
