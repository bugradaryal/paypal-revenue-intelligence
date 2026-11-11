using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class InvoiceTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 7);

    private static Invoice CreateInvoice(InvoiceStatus status, DateOnly? dueDate) =>
        Invoice.Create(
            DataSource.Demo, "INV-1", "0001", null, 250m, "USD", status, status.ToString(),
            dueDate, Now.AddDays(-20), Now.AddDays(-20), Now);

    [Fact]
    public void Sent_invoice_past_its_due_date_is_overdue()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, Today.AddDays(-1));

        Assert.True(invoice.IsOverdue(Today));
    }

    [Fact]
    public void Paid_invoice_is_never_overdue()
    {
        var invoice = CreateInvoice(InvoiceStatus.Paid, Today.AddDays(-10));

        Assert.False(invoice.IsOverdue(Today));
    }

    [Fact]
    public void Invoice_due_today_is_not_overdue_yet()
    {
        var invoice = CreateInvoice(InvoiceStatus.Sent, Today);

        Assert.False(invoice.IsOverdue(Today));
    }
}
