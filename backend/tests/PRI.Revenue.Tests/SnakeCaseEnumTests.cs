using PRI.Application.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class SnakeCaseEnumTests
{
    [Theory]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("FAILED", PaymentStatus.Failed)]
    [InlineData(" completed ", PaymentStatus.Completed)]
    public void Parses_single_word_values(string value, PaymentStatus expected)
    {
        Assert.True(SnakeCaseEnum.TryParse<PaymentStatus>(value, out var parsed));
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void Parses_multi_word_values_in_snake_case()
    {
        Assert.True(SnakeCaseEnum.TryParse<InvoiceStatus>("payment_pending", out var parsed));
        Assert.Equal(InvoiceStatus.PaymentPending, parsed);
    }

    [Theory]
    [InlineData("PaymentPending")]
    [InlineData("nope")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_unknown_values(string? value)
    {
        Assert.False(SnakeCaseEnum.TryParse<InvoiceStatus>(value, out _));
    }
}
