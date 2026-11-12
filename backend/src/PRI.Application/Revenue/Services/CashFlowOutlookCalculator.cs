using PRI.Application.Common.DTOs;
using PRI.Application.Revenue.DTOs;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Services;

public static class CashFlowOutlookCalculator
{
    private const string SummaryCurrency = "USD";
    private const int ForecastDays = 90;

    private sealed record BalanceRow(CashFlowInvoiceFact Fact, decimal Balance);

    public static CashFlowOutlookDto Calculate(IEnumerable<CashFlowInvoiceFact> facts, DateOnly todayUtc)
    {
        var invoices = facts
            .Where(fact => fact.Status is InvoiceStatus.Sent or InvoiceStatus.PaymentPending or InvoiceStatus.PartiallyPaid)
            .Select(fact => new
            {
                fact,
                Balance = Math.Max(0m, fact.Amount - fact.PaidAmount)
            })
            .Where(row => row.Balance > 0m)
            .Select(row => new BalanceRow(row.fact, row.Balance))
            .ToList();

        var usdInvoices = invoices.Where(row => IsCurrency(row.Fact.Currency, SummaryCurrency)).ToList();
        var overdue = usdInvoices.Where(row => row.Fact.DueDate < todayUtc).ToList();
        var unscheduled = usdInvoices.Where(row => row.Fact.DueDate is null).ToList();
        var dueTodayOrLater = usdInvoices
            .Where(row => row.Fact.DueDate is { } dueDate && dueDate >= todayUtc)
            .ToList();
        var sevenDayEnd = todayUtc.AddDays(7);
        var thirtyDayEnd = todayUtc.AddDays(30);
        var ninetyDayEnd = todayUtc.AddDays(ForecastDays);

        var aging = new[]
        {
            AgingBucket("1_7_days", overdue.Where(row => DaysOverdue(row.Fact.DueDate, todayUtc) <= 7)),
            AgingBucket("8_30_days", overdue.Where(row => DaysOverdue(row.Fact.DueDate, todayUtc) is >= 8 and <= 30)),
            AgingBucket("31_plus_days", overdue.Where(row => DaysOverdue(row.Fact.DueDate, todayUtc) > 30))
        };

        var weeklySchedule = Enumerable.Range(0, (ForecastDays + 6) / 7)
            .Select(index =>
            {
                var from = todayUtc.AddDays(index * 7);
                var toExclusive = todayUtc.AddDays(Math.Min((index + 1) * 7, ForecastDays));
                var bucket = dueTodayOrLater
                    .Where(row => row.Fact.DueDate is { } dueDate && dueDate >= from && dueDate < toExclusive)
                    .ToList();
                return new CashFlowWeekDto(from, toExclusive, Usd(bucket.Sum(row => row.Balance)), bucket.Count);
            })
            .ToList();

        var foreignCurrencyExposure = invoices
            .Where(row => !IsCurrency(row.Fact.Currency, SummaryCurrency))
            .GroupBy(row => row.Fact.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                group.Key,
                Amount = group.Sum(row => row.Balance),
                Count = group.Count()
            })
            .OrderByDescending(group => group.Amount)
            .Select(group => new CurrencyExposureDto(group.Key, MoneyDto.From(group.Amount, group.Key), group.Count))
            .ToList();

        var priorityInvoices = invoices
            .OrderBy(row => row.Fact.DueDate is { } dueDate && dueDate < todayUtc ? 0 : 1)
            .ThenBy(row => row.Fact.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(row => row.Balance)
            .Take(12)
            .Select(row => new CashFlowInvoiceDto(
                row.Fact.Id,
                row.Fact.InvoiceNumber,
                row.Fact.CustomerName,
                MoneyDto.From(row.Balance, row.Fact.Currency),
                row.Fact.Status,
                row.Fact.DueDate,
                DaysOverdue(row.Fact.DueDate, todayUtc),
                row.Fact.DataSource))
            .ToList();

        return new CashFlowOutlookDto(
            todayUtc,
            Usd(usdInvoices.Sum(row => row.Balance)),
            usdInvoices.Count,
            Usd(overdue.Sum(row => row.Balance)),
            overdue.Count,
            SumDueBy(sevenDayEnd),
            CountDueBy(sevenDayEnd),
            SumDueBy(thirtyDayEnd),
            CountDueBy(thirtyDayEnd),
            SumDueBy(ninetyDayEnd),
            CountDueBy(ninetyDayEnd),
            Usd(unscheduled.Sum(row => row.Balance)),
            unscheduled.Count,
            aging,
            weeklySchedule,
            foreignCurrencyExposure,
            priorityInvoices);

        MoneyDto SumDueBy(DateOnly endExclusive) =>
            Usd(dueTodayOrLater
                .Where(row => row.Fact.DueDate is { } dueDate && dueDate < endExclusive)
                .Sum(row => row.Balance));

        int CountDueBy(DateOnly endExclusive) =>
            dueTodayOrLater.Count(row => row.Fact.DueDate is { } dueDate && dueDate < endExclusive);
    }

    private static OverdueAgingBucketDto AgingBucket(string key, IEnumerable<BalanceRow> rows)
    {
        var bucket = rows.ToList();
        return new OverdueAgingBucketDto(key, Usd(bucket.Sum(row => row.Balance)), bucket.Count);
    }

    private static int? DaysOverdue(DateOnly? dueDate, DateOnly todayUtc) =>
        dueDate is { } date && date < todayUtc ? todayUtc.DayNumber - date.DayNumber : null;

    private static bool IsCurrency(string currency, string expected) =>
        string.Equals(currency, expected, StringComparison.OrdinalIgnoreCase);

    private static MoneyDto Usd(decimal amount) => MoneyDto.From(amount, SummaryCurrency);
}
