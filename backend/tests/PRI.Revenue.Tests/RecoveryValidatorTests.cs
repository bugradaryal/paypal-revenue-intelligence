using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Validators;

namespace PRI.Revenue.Tests;

public class RecoveryValidatorTests
{
    [Theory]
    [InlineData("approve", true)]
    [InlineData("REJECT", true)]
    [InlineData("maybe", false)]
    [InlineData("", false)]
    public void Only_approve_and_reject_are_valid_decisions(string decision, bool valid)
    {
        Assert.Equal(valid, new SuggestionDecisionRequestValidator().Validate(new SuggestionDecisionRequest(decision)).IsValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(90, true)]
    [InlineData(0, false)]
    [InlineData(91, false)]
    public void Insight_days_must_be_between_one_and_ninety(int days, bool valid)
    {
        Assert.Equal(valid, new GenerateInsightQueryValidator().Validate(new GenerateInsightQuery { Days = days }).IsValid);
    }

    [Fact]
    public void Suggestion_list_filters_accept_snake_case_statuses_and_reject_unknown_ones()
    {
        var validator = new SuggestionListQueryValidator();

        Assert.True(validator.Validate(new SuggestionListQuery { Status = ["proposed", "executed"] }).IsValid);
        Assert.False(validator.Validate(new SuggestionListQuery { Status = ["nope"] }).IsValid);
        Assert.False(validator.Validate(new SuggestionListQuery { SortBy = "customer" }).IsValid);
        Assert.False(validator.Validate(new SuggestionListQuery { PageSize = 101 }).IsValid);
    }

    [Fact]
    public void Issue_list_filters_accept_snake_case_types_and_statuses()
    {
        var validator = new IssueListQueryValidator();

        Assert.True(validator.Validate(new IssueListQuery { Type = ["failed_payment", "overdue_invoice"], Status = ["open"] }).IsValid);
        Assert.False(validator.Validate(new IssueListQuery { Type = ["refund"] }).IsValid);
        Assert.False(validator.Validate(new IssueListQuery { SortBy = "x" }).IsValid);
    }
}
