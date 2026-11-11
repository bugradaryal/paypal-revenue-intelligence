using Microsoft.Extensions.Logging.Abstractions;
using PRI.Application.Common;
using PRI.Application.Common.Interfaces;
using PRI.Application.Revenue.Interfaces;
using PRI.Application.Revenue.Services;
using PRI.Domain.Revenue.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Revenue.Tests;

public class NumberGuardTests
{
    [Theory]
    [InlineData("The 125.50 USD payment failed.", "amount: 125.50 USD", true)]
    [InlineData("Der Betrag 125,50 USD ist offen.", "amount: 125.50 USD", true)]
    [InlineData("The payment of 125 USD.", "amount: 125.00 USD", true)]
    [InlineData("The payment of 125.5 USD.", "amount: 125.50 USD", true)]
    [InlineData("Failed 3 days ago.", "days outstanding: 3", true)]
    [InlineData("No numbers here.", "amount: 10.00 USD", true)]
    [InlineData("The payment of 126 USD.", "amount: 125.50 USD", false)]
    [InlineData("Expect to recover 90% of it.", "amount: 125.50 USD", false)]
    [InlineData("About 2 sentences.", "amount: 125.50 USD", false)]
    public void Output_may_only_contain_numbers_present_in_the_facts(string output, string source, bool allowed)
    {
        Assert.Equal(allowed, NumberGuard.UsesOnlyNumbersFrom(output, source));
    }
}

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", "tr")]
    [InlineData("fr-FR,fr;q=0.9,de;q=0.8", "de")]
    [InlineData("en;q=0.5,es;q=0.9", "es")]
    [InlineData("DE", "de")]
    [InlineData("fr", "en")]
    [InlineData("*", "en")]
    [InlineData("tr;q=0", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void The_best_supported_language_is_chosen_with_english_as_default(string? header, string expected)
    {
        Assert.Equal(expected, SupportedLanguages.Resolve(header));
    }
}

public class RationaleTemplatesTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("tr")]
    [InlineData("de")]
    [InlineData("es")]
    public void Templates_exist_for_every_language_and_action_and_contain_the_given_numbers(string language)
    {
        foreach (var type in Enum.GetValues<RecoveryActionType>())
        {
            var text = RationaleTemplates.Build(type, 125.5m, "USD", PaymentFailureCode.CardDeclined, 7, language);

            Assert.Contains("125.50", text);
            Assert.Contains("7", text);
            Assert.DoesNotContain("{", text);
        }
    }

    [Fact]
    public void Templates_only_use_numbers_they_were_given()
    {
        var text = RationaleTemplates.Build(RecoveryActionType.RetryPayment, 125.5m, "USD", PaymentFailureCode.CardDeclined, 7, "en");

        Assert.True(NumberGuard.UsesOnlyNumbersFrom(text, "125.50 USD 7 days"));
    }

    [Fact]
    public void An_unknown_language_falls_back_to_english()
    {
        var text = RationaleTemplates.Build(RecoveryActionType.SendReminder, 10m, "USD", null, 2, "xx");

        Assert.Contains("reminder", text);
    }
}

public class RationaleWriterTests
{
    private static readonly RationaleFacts Facts = new(
        RecoveryActionType.RetryPayment, IssueType.FailedPayment, 125.50m, "USD", PaymentFailureCode.CardDeclined, 3);

    private static (RationaleWriter Writer, FakeAiGateway Gateway) Create(Func<AiTextRequest, AiTextResult> behavior)
    {
        var gateway = new FakeAiGateway { Behavior = behavior };

        return (new RationaleWriter(gateway, NullLogger<RationaleWriter>.Instance), gateway);
    }

    [Fact]
    public async Task A_valid_ai_text_is_used_and_marked_as_ai()
    {
        var requestId = Guid.NewGuid();
        var (writer, gateway) = Create(_ => new AiTextResult("The 125.50 USD charge was declined 3 days ago; a retry may work.", false, requestId));

        var result = await writer.WriteRationaleAsync(Facts, "en", CancellationToken.None);

        Assert.Equal(ContentSource.Ai, result.Source);
        Assert.Equal(requestId, result.GatewayRequestId);
        Assert.Contains("retry", result.Text);
        Assert.Single(gateway.Requests);
    }

    [Fact]
    public async Task The_prompt_carries_the_computed_facts_and_the_language_and_no_personal_data()
    {
        var (writer, gateway) = Create(_ => new AiTextResult("ok 125.50 3", false, Guid.NewGuid()));

        await writer.WriteRationaleAsync(Facts, "tr", CancellationToken.None);

        var request = gateway.Requests.Single();
        Assert.Contains("125.50 USD", request.UserPrompt);
        Assert.Contains("days outstanding: 3", request.UserPrompt);
        Assert.Contains("CardDeclined", request.UserPrompt);
        Assert.Contains("Turkish", request.SystemPrompt);
        Assert.Contains("Do not mention customer names", request.SystemPrompt);
        Assert.DoesNotContain("@", request.UserPrompt);
    }

    [Fact]
    public async Task A_text_with_an_invented_number_is_replaced_by_the_template()
    {
        var (writer, _) = Create(_ => new AiTextResult("You will recover 95% of the 125.50 USD.", false, Guid.NewGuid()));

        var result = await writer.WriteRationaleAsync(Facts, "en", CancellationToken.None);

        Assert.Equal(ContentSource.Rule, result.Source);
        Assert.Null(result.GatewayRequestId);
        Assert.Contains("125.50", result.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_empty_ai_text_falls_back_to_the_template(string content)
    {
        var (writer, _) = Create(_ => new AiTextResult(content, false, Guid.NewGuid()));

        Assert.Equal(ContentSource.Rule, (await writer.WriteRationaleAsync(Facts, "en", CancellationToken.None)).Source);
    }

    [Fact]
    public async Task A_truncated_or_overlong_ai_text_falls_back_to_the_template()
    {
        var (truncated, _) = Create(_ => new AiTextResult("Half a sent", true, Guid.NewGuid()));
        var (tooLong, _) = Create(_ => new AiTextResult(new string('a', 700), false, Guid.NewGuid()));

        Assert.Equal(ContentSource.Rule, (await truncated.WriteRationaleAsync(Facts, "en", CancellationToken.None)).Source);
        Assert.Equal(ContentSource.Rule, (await tooLong.WriteRationaleAsync(Facts, "en", CancellationToken.None)).Source);
    }

    [Theory]
    [InlineData("budget_exceeded")]
    [InlineData("openai_not_configured")]
    public async Task Gateway_errors_fall_back_to_the_template_in_the_requested_language(string code)
    {
        var (writer, _) = Create(_ => throw new BusinessRuleException(code, "nope"));

        var result = await writer.WriteRationaleAsync(Facts, "de", CancellationToken.None);

        Assert.Equal(ContentSource.Rule, result.Source);
        Assert.Contains("fehlgeschlagen", result.Text);
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed()
    {
        var (writer, _) = Create(_ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => writer.WriteRationaleAsync(Facts, "en", CancellationToken.None));
    }

    [Fact]
    public async Task Insight_commentary_may_only_repeat_the_given_numbers()
    {
        var facts = new InsightFacts(
            30, 1000m, 200m, 50m, 40, 5, 300m, 150m, 2, 400m,
            [new FailureReasonFact(PaymentFailureCode.CardDeclined, 3, 120m)], 800m, 100m);

        var (good, goodGateway) = Create(_ => new AiTextResult(
            "Over the last 30 days 1000.00 USD was collected while 200.00 USD failed.", false, Guid.NewGuid()));
        var (bad, _) = Create(_ => new AiTextResult("Revenue grew 25% to 1000.00 USD.", false, Guid.NewGuid()));

        var accepted = await good.WriteInsightCommentaryAsync(facts, "en", CancellationToken.None);
        var rejected = await bad.WriteInsightCommentaryAsync(facts, "en", CancellationToken.None);

        Assert.Equal(ContentSource.Ai, accepted.Source);
        Assert.Contains("failure rate: 12.5%", goodGateway.Requests.Single().UserPrompt);
        Assert.Contains("recovery rate: 50.0%", goodGateway.Requests.Single().UserPrompt);
        Assert.Equal(ContentSource.Rule, rejected.Source);
    }
}
