using PRI.Application.Common.DTOs;
using PRI.Application.Common.Validators;
using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Validators;

namespace PRI.Revenue.Tests;

public class QueryValidatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Date_range_requires_both_ends()
    {
        var result = new DateRangeQueryValidator().Validate(new DateRangeQuery());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Date_range_must_be_ordered_and_at_most_366_days()
    {
        var validator = new DateRangeQueryValidator();

        Assert.True(validator.Validate(new DateRangeQuery { From = Start, To = Start.AddDays(30) }).IsValid);
        Assert.False(validator.Validate(new DateRangeQuery { From = Start, To = Start }).IsValid);
        Assert.False(validator.Validate(new DateRangeQuery { From = Start, To = Start.AddDays(400) }).IsValid);
    }

    [Fact]
    public void Payment_list_rejects_unknown_status_and_oversized_pages()
    {
        var validator = new PaymentListQueryValidator();

        Assert.True(validator.Validate(new PaymentListQuery { Status = ["failed", "pending"] }).IsValid);
        Assert.False(validator.Validate(new PaymentListQuery { Status = ["bogus"] }).IsValid);
        Assert.False(validator.Validate(new PaymentListQuery { PageSize = 101 }).IsValid);
        Assert.False(validator.Validate(new PaymentListQuery { SortBy = "customer" }).IsValid);
    }

    [Fact]
    public void Sync_days_are_limited_to_90()
    {
        var validator = new SyncRequestValidator();

        Assert.True(validator.Validate(new SyncRequest { Days = 90 }).IsValid);
        Assert.False(validator.Validate(new SyncRequest { Days = 0 }).IsValid);
        Assert.False(validator.Validate(new SyncRequest { Days = 91 }).IsValid);
    }
}
