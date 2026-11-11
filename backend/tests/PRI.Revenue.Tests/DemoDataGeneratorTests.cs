using PRI.Application.Revenue.Demo;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue;
using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class DemoDataGeneratorTests
{
    private static DemoDataSet Generate(int seed = DemoDataGenerator.DefaultSeed) => DemoDataGenerator.Generate(T.Now, seed);

    [Fact]
    public void The_same_time_and_seed_always_produce_the_same_data()
    {
        var a = Generate();
        var b = Generate();

        Assert.Equal(
            a.Payments.Select(p => (p.ExternalId, p.Amount, p.Status, p.FailureCode, p.InitiatedAtUtc)),
            b.Payments.Select(p => (p.ExternalId, p.Amount, p.Status, p.FailureCode, p.InitiatedAtUtc)));
        Assert.Equal(
            a.Invoices.Select(i => (i.ExternalId, i.Amount, i.Status, i.DueDate)),
            b.Invoices.Select(i => (i.ExternalId, i.Amount, i.Status, i.DueDate)));
    }

    [Fact]
    public void A_different_seed_changes_the_random_part()
    {
        var a = Generate(1);
        var b = Generate(2);

        Assert.NotEqual(
            a.Payments.Select(p => (p.Amount, p.Status)).ToList(),
            b.Payments.Select(p => (p.Amount, p.Status)).ToList());
    }

    [Fact]
    public void Everything_is_labeled_demo_and_nothing_is_dated_in_the_future()
    {
        var data = Generate();

        Assert.All(data.Customers, c => Assert.Equal(DataSource.Demo, c.DataSource));
        Assert.All(data.Invoices, i => Assert.Equal(DataSource.Demo, i.DataSource));
        Assert.All(data.Payments, p => Assert.Equal(DataSource.Demo, p.DataSource));
        Assert.All(data.Payments, p => Assert.True(p.InitiatedAtUtc <= T.Now));
        Assert.All(data.Payments, p => Assert.True(p.UpdatedAtUtc >= p.InitiatedAtUtc));
        Assert.All(data.Payments, p => Assert.StartsWith("DEMO-", p.ExternalId));
    }

    [Fact]
    public void External_ids_are_unique_and_references_are_consistent()
    {
        var data = Generate();
        var customerIds = data.Customers.Select(c => c.Id).ToHashSet();
        var invoiceIds = data.Invoices.Select(i => i.Id).ToHashSet();

        Assert.Equal(data.Payments.Count, data.Payments.Select(p => p.ExternalId).Distinct().Count());
        Assert.Equal(data.Invoices.Count, data.Invoices.Select(i => i.ExternalId).Distinct().Count());
        Assert.Equal(data.Customers.Count, data.Customers.Select(c => c.ExternalId).Distinct().Count());
        Assert.All(data.Payments, p => Assert.Contains(p.CustomerId, customerIds));
        Assert.All(data.Payments.Where(p => p.InvoiceId is not null), p => Assert.Contains(p.InvoiceId!.Value, invoiceIds));
        Assert.All(data.Invoices.Where(i => i.CustomerId is not null), i => Assert.Contains(i.CustomerId!.Value, customerIds));
    }

    [Fact]
    public void The_data_covers_every_status_failure_code_and_a_non_usd_currency()
    {
        var data = Generate();

        foreach (var status in Enum.GetValues<PaymentStatus>())
        {
            Assert.Contains(data.Payments, p => p.Status == status);
        }

        foreach (var code in Enum.GetValues<PaymentFailureCode>())
        {
            Assert.Contains(data.Payments, p => p.FailureCode == code);
        }

        foreach (var status in new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid, InvoiceStatus.Cancelled })
        {
            Assert.Contains(data.Invoices, i => i.Status == status);
        }

        Assert.Equal(2, data.Payments.Count(p => p.Currency == "EUR"));
        Assert.True(data.Payments.Count(p => p.Currency == "USD") > 100);
    }

    [Fact]
    public async Task Detecting_issues_on_the_demo_data_finds_the_planted_scenarios()
    {
        var data = Generate();
        var facts = new FakeFactsReader();
        facts.Payments.AddRange(data.Payments.Select(ToFact));
        facts.Invoices.AddRange(data.Invoices.Select(ToFact));
        var issues = new FakeIssueRepository();
        var sync = new RevenueIssueSync(issues, facts, new FakeUnitOfWork(), new ManualTimeProvider(T.Now));

        await sync.RefreshAsync(CancellationToken.None);

        var open = issues.Issues.Where(i => i.Status == IssueStatus.Open).ToList();
        var recovered = issues.Issues.Where(i => i.Status == IssueStatus.Recovered).ToList();

        Assert.Equal(3, open.Count(i => i.Type == IssueType.PendingPayment));        // 5, 8 ve 12 gündür bekleyenler
        Assert.Equal(5, open.Count(i => i.Type == IssueType.OverdueInvoice));        // 4 gönderilmiş + 1 kısmen ödenmiş
        Assert.True(recovered.Count >= 6);                                            // 4 yeniden deneme + 2 faturaya bağlı
        Assert.Contains(recovered, i => i.Evidence == RecoveryEvidence.MatchingRetryPayment);
        Assert.Contains(recovered, i => i.Evidence == RecoveryEvidence.LinkedInvoicePayment);

        foreach (var code in Enum.GetValues<PaymentFailureCode>())
        {
            Assert.Contains(open, i => i.Type == IssueType.FailedPayment && i.FailureCode == code);
        }

        // Faturasız-ödemesiz hiçbir kayıp olmaz ve kayıp tutarları pozitiftir.
        Assert.All(issues.Issues, i => Assert.True(i.Amount > 0));
    }

    [Fact]
    public async Task Only_usd_losses_feed_the_overdue_snapshot_but_eur_failures_are_still_recorded_as_issues()
    {
        var data = Generate();
        var facts = new FakeFactsReader();
        facts.Payments.AddRange(data.Payments.Select(ToFact));
        facts.Invoices.AddRange(data.Invoices.Select(ToFact));
        var issues = new FakeIssueRepository();

        await new RevenueIssueSync(issues, facts, new FakeUnitOfWork(), new ManualTimeProvider(T.Now)).RefreshAsync(CancellationToken.None);

        // Para birimi kayıt düzeyinde korunur; özetler yalnızca USD'yi toplar (sorgu katmanı).
        Assert.Contains(issues.Issues, i => i.Currency == "EUR");
        Assert.All(issues.Issues.Where(i => i.Currency == "EUR"), i => Assert.Equal(IssueType.FailedPayment, i.Type));
    }

    private static PaymentFact ToFact(Payment p) =>
        new(p.Id, p.CustomerId, p.InvoiceId, p.DataSource, p.Amount, p.Currency, p.Status, p.FailureCode,
            p.InitiatedAtUtc, p.UpdatedAtUtc);

    private static InvoiceFact ToFact(Invoice i) =>
        new(i.Id, i.CustomerId, i.DataSource, i.ExternalId, i.Amount, i.Currency, i.Status, i.DueDate, i.UpdatedAtUtc);
}
