using PRI.Domain.AiGateway;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Enums;

namespace PRI.AiGateway.Tests;

public class SemanticCacheKeyTests
{
    [Fact]
    public void Query_normalization_ignores_case_and_whitespace()
    {
        Assert.Equal(
            SemanticCacheKey.ComputeQueryHash(SemanticCacheKey.NormalizeQuery("Refund  Policy")),
            SemanticCacheKey.ComputeQueryHash(SemanticCacheKey.NormalizeQuery(" refund policy ")));
    }

    [Fact]
    public void Numbers_fingerprint_differs_when_numbers_differ_and_is_stable_otherwise()
    {
        var order123 = SemanticCacheKey.ComputeNumbersFingerprint("Sipariş 123 nerede?");
        var order456 = SemanticCacheKey.ComputeNumbersFingerprint("Sipariş 456 nerede?");
        var again = SemanticCacheKey.ComputeNumbersFingerprint("123 numaralı siparişim nerede");

        Assert.NotEqual(order123, order456);
        Assert.Equal(order123, again);
    }

    [Fact]
    public void Number_order_matters()
    {
        Assert.NotEqual(
            SemanticCacheKey.ComputeNumbersFingerprint("5'ten 10'a"),
            SemanticCacheKey.ComputeNumbersFingerprint("10'dan 5'e"));
    }

    private static string Context(string? model = "auto", string? task = null, double? temperature = null, string system = "Sen bir asistansın.") =>
        SemanticCacheKey.ComputeContextHash(model, task, null, temperature, null, [("system", system)]);

    [Fact]
    public void Context_hash_changes_with_model_task_temperature_or_earlier_messages()
    {
        var baseline = Context();

        Assert.Equal(baseline, Context());
        Assert.Equal(baseline, Context(model: null));          // boş model = "auto"
        Assert.NotEqual(baseline, Context(model: "gpt-6-luna"));
        Assert.NotEqual(baseline, Context(task: "analysis"));
        Assert.NotEqual(baseline, Context(temperature: 0.2));
        Assert.NotEqual(baseline, Context(system: "Sen bir hukuk asistanısın."));
    }

    [Fact]
    public void Context_hash_separates_provider_pinned_automatic_requests()
    {
        var openAi = SemanticCacheKey.ComputeContextHash(
            "auto", null, null, null, null, [("system", "assistant")], AiProvider.Openai);
        var gemini = SemanticCacheKey.ComputeContextHash(
            "auto", null, null, null, null, [("system", "assistant")], AiProvider.Gemini);

        Assert.NotEqual(openAi, gemini);
    }

    [Fact]
    public void Context_hash_fields_cannot_be_shifted_into_each_other()
    {
        var a = SemanticCacheKey.ComputeContextHash("auto", "ab", null, null, null, [("user", "c")]);
        var b = SemanticCacheKey.ComputeContextHash("auto", "a", null, null, null, [("user", "bc")]);

        Assert.NotEqual(a, b);
    }
}

public class EmbeddingMathTests
{
    [Fact]
    public void Normalized_identical_vectors_have_similarity_one_and_orthogonal_zero()
    {
        var a = EmbeddingMath.Normalize([3f, 4f]);

        Assert.Equal(1d, EmbeddingMath.Similarity(a, EmbeddingMath.Normalize([6f, 8f])), 5);
        Assert.Equal(0d, EmbeddingMath.Similarity(EmbeddingMath.Normalize([1f, 0f]), EmbeddingMath.Normalize([0f, 1f])), 5);
    }

    [Fact]
    public void Vectors_of_different_length_are_not_comparable()
    {
        Assert.Equal(-1d, EmbeddingMath.Similarity([1f, 0f], [1f, 0f, 0f]));
    }

    [Fact]
    public void A_zero_vector_normalizes_to_zeros_without_dividing_by_zero()
    {
        Assert.All(EmbeddingMath.Normalize([0f, 0f]), x => Assert.Equal(0f, x));
    }
}

public class AiBudgetTests
{
    private static AiBudget Budget(int warn = 80, bool enforce = true) =>
        AiBudget.Create(10m, warn, enforce, TestData.Now);

    [Theory]
    [InlineData("0", BudgetLevel.Ok)]
    [InlineData("7.99", BudgetLevel.Ok)]
    [InlineData("8", BudgetLevel.Warning)]
    [InlineData("9.99", BudgetLevel.Warning)]
    [InlineData("10", BudgetLevel.Exceeded)]
    [InlineData("25", BudgetLevel.Exceeded)]
    public void Level_follows_spent_against_limit_and_warning_percent(string spent, BudgetLevel expected)
    {
        Assert.Equal(expected, Budget().Evaluate(decimal.Parse(spent, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Spending_is_blocked_only_when_exceeded_and_enforced()
    {
        Assert.True(Budget().CanSpend(9.99m));
        Assert.False(Budget().CanSpend(10m));
        Assert.True(Budget(enforce: false).CanSpend(50m));
    }

    [Theory]
    [InlineData(0, 80)]
    [InlineData(-1, 80)]
    [InlineData(10, 0)]
    [InlineData(10, 101)]
    public void Invalid_values_are_rejected(int limit, int warn)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AiBudget.Create(limit, warn, true, TestData.Now));
    }

    [Fact]
    public void Period_is_the_utc_calendar_month()
    {
        var (start, end) = AiBudget.PeriodContaining(new DateTime(2026, 12, 31, 23, 59, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), end);
    }
}
