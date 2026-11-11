using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Validators;

namespace PRI.AiGateway.Tests;

public class CompletionRequestValidatorTests
{
    private static readonly CompletionRequestValidator Validator = new();

    private static CompletionRequest Request(
        string? model = null,
        IReadOnlyList<CompletionMessageDto>? messages = null,
        int? maxOutputTokens = null,
        double? temperature = null,
        string? reasoningEffort = null) =>
        new(
            model,
            null, // TaskType: otomatik yönlendirmede varsayýlan görev tipi
            messages ?? [new CompletionMessageDto("user", "Merhaba")],
            maxOutputTokens,
            temperature,
            reasoningEffort,
            null, // Cache: varsayýlan true
            null  // OptimizePrompt: varsayýlan true
        );

    [Fact]
    public void A_minimal_request_is_valid()
    {
        Assert.True(Validator.Validate(Request()).IsValid);
    }

    [Fact]
    public void Requires_at_least_one_message()
    {
        Assert.False(Validator.Validate(Request(messages: [])).IsValid);
    }

    [Fact]
    public void Rejects_unknown_roles_and_empty_content()
    {
        Assert.False(Validator.Validate(Request(messages: [new CompletionMessageDto("tool", "x")])).IsValid);
        Assert.False(Validator.Validate(Request(messages: [new CompletionMessageDto("user", "")])).IsValid);
    }

    [Fact]
    public void Rejects_a_total_input_that_could_reach_long_context_pricing()
    {
        var big = new string('a', 100_000);
        var messages = Enumerable.Range(0, 5).Select(_ => new CompletionMessageDto("user", big)).ToList();

        Assert.False(Validator.Validate(Request(messages: messages)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32_769)]
    public void Rejects_out_of_range_output_limits(int maxOutputTokens)
    {
        Assert.False(Validator.Validate(Request(maxOutputTokens: maxOutputTokens)).IsValid);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    public void Rejects_out_of_range_temperatures(double temperature)
    {
        Assert.False(Validator.Validate(Request(temperature: temperature)).IsValid);
    }

    [Theory]
    [InlineData("none", true)]
    [InlineData("high", true)]
    [InlineData("turbo", false)]
    public void Validates_reasoning_effort(string effort, bool expected)
    {
        Assert.Equal(expected, Validator.Validate(Request(reasoningEffort: effort)).IsValid);
    }
}
