using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;

namespace PRI.AiGateway.Tests;

public class UsageRecordTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static ProviderModel Luna() =>
        ProviderModel.Create(
            AiProvider.Openai, "gpt-6-luna", "GPT-6 Luna", ModelTier.Economy,
            0.10m, 0.01m, 0.50m, "https://developers.openai.com/api/docs/pricing", new DateOnly(2026, 11, 7), Now);

    [Fact]
    public void Model_calculates_cost_from_its_own_prices()
    {
        Assert.Equal(0.60m, Luna().CalculateCost(1_000_000, 0, 1_000_000));
    }

    [Fact]
    public void Success_record_stores_real_usage_cost_and_a_price_snapshot()
    {
        var model = Luna();

        var record = UsageRecord.Success(Guid.NewGuid(), model, null, false, 1, 1000, 200, 500, null, 120, "chatcmpl-1", Now, Now);

        Assert.Equal(UsageStatus.Success, record.Status);
        Assert.Equal(0.000332m, record.CostUsd);
        Assert.Equal(model.InputPricePerMillionUsd, record.InputPricePerMillionUsd);
        Assert.Equal(model.OutputPricePerMillionUsd, record.OutputPricePerMillionUsd);
        Assert.Equal("gpt-6-luna", record.ModelId);
        Assert.Equal(200, record.CachedInputTokens);
    }

    [Fact]
    public void Failure_record_costs_nothing_and_keeps_the_error_code()
    {
        var record = UsageRecord.Failure(Guid.NewGuid(), Luna(), null, false, 1, "provider_unavailable", 800, Now, Now);

        Assert.Equal(UsageStatus.Failed, record.Status);
        Assert.Equal(0m, record.CostUsd);
        Assert.Equal(0, record.InputTokens);
        Assert.Equal("provider_unavailable", record.ErrorCode);
    }
}
