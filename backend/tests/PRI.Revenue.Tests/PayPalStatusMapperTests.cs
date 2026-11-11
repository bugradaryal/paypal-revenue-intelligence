using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class PayPalStatusMapperTests
{
    [Theory]
    [InlineData("S", PaymentStatus.Completed)]
    [InlineData("P", PaymentStatus.Pending)]
    [InlineData("D", PaymentStatus.Failed)]
    [InlineData("V", PaymentStatus.Refunded)]
    [InlineData("s", PaymentStatus.Completed)]
    public void Known_transaction_codes_are_mapped(string code, PaymentStatus expected)
    {
        Assert.True(PayPalStatusMapper.TryMapTransactionStatus(code, out var status));
        Assert.Equal(expected, status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("X")]
    public void Unknown_transaction_codes_are_not_guessed(string? code)
    {
        Assert.False(PayPalStatusMapper.TryMapTransactionStatus(code, out _));
    }

    [Theory]
    [InlineData("SENT", InvoiceStatus.Sent)]
    [InlineData("UNPAID", InvoiceStatus.Sent)]
    [InlineData("MARKED_AS_PAID", InvoiceStatus.Paid)]
    [InlineData("PARTIALLY_REFUNDED", InvoiceStatus.Refunded)]
    [InlineData("CANCELLED", InvoiceStatus.Cancelled)]
    public void Known_invoice_statuses_are_mapped(string providerStatus, InvoiceStatus expected)
    {
        Assert.True(PayPalStatusMapper.TryMapInvoiceStatus(providerStatus, out var status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void Unknown_invoice_status_is_not_guessed()
    {
        Assert.False(PayPalStatusMapper.TryMapInvoiceStatus("SOMETHING_NEW", out _));
    }
}
