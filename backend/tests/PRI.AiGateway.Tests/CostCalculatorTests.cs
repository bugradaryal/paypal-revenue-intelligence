using PRI.Domain.AiGateway;

namespace PRI.AiGateway.Tests;

public class CostCalculatorTests
{
    [Fact]
    public void Splits_input_into_cached_and_uncached_parts()
    {
        // gpt-6-luna fiyatları: 0.10 / 0.01 (önbellekli) / 0.50 (USD, 1M token)
        // (800 * 0.10 + 200 * 0.01 + 500 * 0.50) / 1.000.000
        var cost = CostCalculator.Calculate(1000, 200, 500, 0.10m, 0.01m, 0.50m);

        Assert.Equal(0.000332m, cost);
    }

    [Fact]
    public void Uses_the_normal_input_price_when_no_cached_price_is_known()
    {
        var cost = CostCalculator.Calculate(1000, 400, 0, 2.00m, null, 10.00m);

        Assert.Equal(0.002m, cost);
    }

    [Fact]
    public void Cached_tokens_can_never_exceed_the_input_tokens()
    {
        var clamped = CostCalculator.Calculate(100, 500, 0, 1.00m, 0.10m, 5.00m);
        var exact = CostCalculator.Calculate(100, 100, 0, 1.00m, 0.10m, 5.00m);

        Assert.Equal(exact, clamped);
    }

    [Fact]
    public void One_million_input_and_output_tokens_cost_the_listed_prices()
    {
        var cost = CostCalculator.Calculate(1_000_000, 0, 1_000_000, 10.00m, 1.00m, 50.00m);

        Assert.Equal(60m, cost);
    }

    [Fact]
    public void Zero_usage_costs_nothing()
    {
        Assert.Equal(0m, CostCalculator.Calculate(0, 0, 0, 1.00m, 0.10m, 5.00m));
    }

    [Fact]
    public void Negative_values_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CostCalculator.Calculate(-1, 0, 0, 1m, null, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CostCalculator.Calculate(1, 0, 0, -1m, null, 1m));
    }
}
