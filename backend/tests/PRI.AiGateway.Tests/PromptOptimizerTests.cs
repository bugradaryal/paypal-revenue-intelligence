using PRI.Application.AiGateway.Interfaces;
using PRI.Application.AiGateway.Services;

namespace PRI.AiGateway.Tests;

public class PromptOptimizerTests
{
    private readonly PromptOptimizer _optimizer = new();

    private static IReadOnlyList<ProviderMessage> One(string content, string role = "user") => [new ProviderMessage(role, content)];

    [Fact]
    public void Trailing_spaces_and_excess_blank_lines_are_removed_and_savings_are_measured()
    {
        var result = _optimizer.Optimize(One("Merhaba   \r\n\r\n\r\n\r\nNasılsın?  \n"));

        Assert.Equal("Merhaba\n\nNasılsın?", result.Messages[0].Content);
        Assert.True(result.CharactersSaved > 0);
        Assert.Equal(result.CharactersBefore - result.CharactersAfter, result.CharactersSaved);
        Assert.Equal(result.CharactersSaved / 4, result.EstimatedTokensSaved);
    }

    [Fact]
    public void Code_fences_are_left_untouched()
    {
        const string content = "Önce:  \n```python\ndef f():   \n\n\n\n    return 1  \n```\nSonra";

        var result = _optimizer.Optimize(One(content));

        Assert.Contains("def f():   \n\n\n\n    return 1  \n", result.Messages[0].Content);
        Assert.StartsWith("Önce:\n```python", result.Messages[0].Content);
    }

    [Fact]
    public void Words_and_internal_spacing_are_never_changed()
    {
        var result = _optimizer.Optimize(One("a  b   c"));

        Assert.Equal("a  b   c", result.Messages[0].Content);
        Assert.Equal(0, result.CharactersSaved);
    }

    [Fact]
    public void Duplicate_system_messages_are_collapsed_but_user_messages_are_kept()
    {
        var result = _optimizer.Optimize(
        [
            new ProviderMessage("system", "Sen yardımcı bir asistansın."),
            new ProviderMessage("user", "Selam"),
            new ProviderMessage("system", "Sen yardımcı bir asistansın.  "),
            new ProviderMessage("user", "Selam")
        ]);

        Assert.Equal(3, result.Messages.Count);
        Assert.Single(result.Messages, m => m.Role == "system");
        Assert.Equal(2, result.Messages.Count(m => m.Role == "user"));
    }

    [Fact]
    public void Zero_width_spaces_are_removed_but_emoji_joiners_are_kept()
    {
        var result = _optimizer.Optimize(One("a\u200Bb 👨\u200D👩\u200D👧"));

        Assert.Equal("ab 👨\u200D👩\u200D👧", result.Messages[0].Content);
    }

    [Fact]
    public void A_message_that_would_become_empty_keeps_its_original_content()
    {
        var result = _optimizer.Optimize(One("\u200B\u200B"));

        Assert.Equal("\u200B\u200B", result.Messages[0].Content);
    }

    [Fact]
    public void Optimizing_twice_gives_the_same_result()
    {
        var once = _optimizer.Optimize(One("x  \n\n\n\ny   "));
        var twice = _optimizer.Optimize(once.Messages);

        Assert.Equal(once.Messages[0].Content, twice.Messages[0].Content);
        Assert.Equal(0, twice.CharactersSaved);
    }
}
