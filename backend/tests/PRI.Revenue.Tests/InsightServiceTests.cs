using PRI.Application.Revenue.DTOs;
using PRI.Application.Revenue.Interfaces;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;

namespace PRI.Revenue.Tests;

public class InsightServiceTests
{
    private sealed class Harness
    {
        public Harness()
        {
            Queries.InsightFactsQueue.Enqueue(new InsightFacts(
                0, 1000m, 200m, 50m, 40, 5, 300m, 150m, 2, 400m,
                [new FailureReasonFact(PaymentFailureCode.CardDeclined, 3, 120m)], 0m, 0m));
            Queries.InsightFactsQueue.Enqueue(new InsightFacts(
                0, 800m, 100m, 0m, 30, 2, 0m, 0m, 0, 0m, [], 0m, 0m));

            Service = new InsightService(Sync, Queries, Writer, Insights, UnitOfWork, new ManualTimeProvider(T.Now));
        }

        public FakeIssueSync Sync { get; } = new();

        public FakeRevenueQueries Queries { get; } = new();

        public FakeRationaleWriter Writer { get; } = new();

        public FakeInsightRepository Insights { get; } = new();

        public FakeUnitOfWork UnitOfWork { get; } = new();

        public InsightService Service { get; }
    }

    [Fact]
    public async Task Generate_computes_metrics_in_code_and_compares_with_the_previous_period()
    {
        var h = new Harness();

        var insight = await h.Service.GenerateAsync(30, "en", CancellationToken.None);

        var m = insight.Metrics;
        Assert.Equal("1000.00", m.Revenue.Amount);
        Assert.Equal("200.00", m.FailedAmount.Amount);
        Assert.Equal(0.125, m.FailureRate);          // 5 / 40
        Assert.Equal(0.5, m.RecoveryRate);           // 150 / 300
        Assert.Equal(2, m.OverdueInvoiceCount);
        Assert.Equal("400.00", m.OverdueInvoiceAmount.Amount);
        Assert.Equal(PaymentFailureCode.CardDeclined, Assert.Single(m.TopFailureReasons).Code);
        Assert.Equal("800.00", m.Previous.Revenue.Amount);
        Assert.Equal(25.0, m.Previous.RevenueChangePercent);       // (1000-800)/800
        Assert.Equal(100.0, m.Previous.FailedAmountChangePercent); // (200-100)/100
    }

    [Fact]
    public async Task A_previous_period_of_zero_gives_no_percentage_instead_of_infinity()
    {
        var h = new Harness();
        h.Queries.InsightFactsQueue.Clear();
        h.Queries.InsightFactsQueue.Enqueue(new InsightFacts(0, 500m, 0m, 0m, 0, 0, 0m, 0m, 0, 0m, [], 0m, 0m));
        h.Queries.InsightFactsQueue.Enqueue(new InsightFacts(0, 0m, 0m, 0m, 0, 0, 0m, 0m, 0, 0m, [], 0m, 0m));

        var insight = await h.Service.GenerateAsync(7, "en", CancellationToken.None);

        Assert.Null(insight.Metrics.Previous.RevenueChangePercent);
        Assert.Equal(0d, insight.Metrics.FailureRate);
        Assert.Equal(0d, insight.Metrics.RecoveryRate);
    }

    [Fact]
    public async Task The_period_includes_today_and_ends_at_the_next_midnight()
    {
        var h = new Harness();

        var insight = await h.Service.GenerateAsync(30, "en", CancellationToken.None);

        Assert.Equal(new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc), insight.PeriodTo);
        Assert.Equal(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc), insight.PeriodFrom);
    }

    [Fact]
    public async Task Generate_refreshes_issues_stores_the_insight_and_keeps_commentary_source_and_language()
    {
        var h = new Harness();
        h.Writer.Source = ContentSource.Ai;

        var insight = await h.Service.GenerateAsync(30, "de", CancellationToken.None);

        Assert.Equal(1, h.Sync.Calls);
        Assert.Single(h.Insights.Insights);
        Assert.Equal(1, h.UnitOfWork.SaveCount);
        Assert.Equal("insight-commentary", insight.Commentary);
        Assert.Equal("de", insight.CommentaryLanguage);
        Assert.Equal(ContentSource.Ai, insight.CommentarySource);
    }

    [Fact]
    public async Task Stored_insights_round_trip_through_json_when_listed()
    {
        var h = new Harness();
        var generated = await h.Service.GenerateAsync(30, "en", CancellationToken.None);

        var page = await h.Service.ListAsync(new InsightListQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        var listed = Assert.Single(page.Items);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(generated.Id, listed.Id);
        Assert.Equal(generated.Metrics.Revenue, listed.Metrics.Revenue);
        Assert.Equal(generated.Metrics.FailureRate, listed.Metrics.FailureRate);
        Assert.Equal(PaymentFailureCode.CardDeclined, Assert.Single(listed.Metrics.TopFailureReasons).Code);
        Assert.Equal(generated.Metrics.Previous.RevenueChangePercent, listed.Metrics.Previous.RevenueChangePercent);
    }
}
