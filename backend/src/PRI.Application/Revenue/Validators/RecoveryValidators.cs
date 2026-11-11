using FluentValidation;
using PRI.Application.Common;
using PRI.Application.Revenue.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Validators;

public sealed class SuggestionListQueryValidator : AbstractValidator<SuggestionListQuery>
{
    private static readonly string[] SortFields = ["createdAt", "expectedRecovery"];

    public SuggestionListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleForEach(x => x.Status)
            .Must(value => SnakeCaseEnum.TryParse<RecoveryActionStatus>(value, out _))
            .WithMessage("Unknown suggestion status '{PropertyValue}'.");
        RuleFor(x => x.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: createdAt, expectedRecovery.");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
    }
}

public sealed class SuggestionDecisionRequestValidator : AbstractValidator<SuggestionDecisionRequest>
{
    public SuggestionDecisionRequestValidator()
    {
        RuleFor(x => x.Decision)
            .Must(value => value is not null && (value.Equals("approve", StringComparison.OrdinalIgnoreCase)
                                                 || value.Equals("reject", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("decision must be 'approve' or 'reject'.");
    }
}

public sealed class IssueListQueryValidator : AbstractValidator<IssueListQuery>
{
    private static readonly string[] SortFields = ["problemSince", "amount"];

    public IssueListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleForEach(x => x.Status)
            .Must(value => SnakeCaseEnum.TryParse<IssueStatus>(value, out _))
            .WithMessage("Unknown issue status '{PropertyValue}'.");
        RuleForEach(x => x.Type)
            .Must(value => SnakeCaseEnum.TryParse<IssueType>(value, out _))
            .WithMessage("Unknown issue type '{PropertyValue}'.");
        RuleFor(x => x.SortBy).Must(value => SortFields.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("sortBy must be one of: problemSince, amount.");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc")
            .WithMessage("sortDir must be 'asc' or 'desc'.");
    }
}

public sealed class InsightListQueryValidator : AbstractValidator<InsightListQuery>
{
    public InsightListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}

public sealed class GenerateInsightQueryValidator : AbstractValidator<GenerateInsightQuery>
{
    public GenerateInsightQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 90);
    }
}
