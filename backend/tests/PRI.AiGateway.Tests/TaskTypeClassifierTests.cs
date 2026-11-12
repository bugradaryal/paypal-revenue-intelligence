using PRI.Application.AiGateway.Services;

namespace PRI.AiGateway.Tests;

public sealed class TaskTypeClassifierTests
{
    [Theory]
    [InlineData("Summarize these payment notes.", "summarization")]
    [InlineData("Bu ödeme hatalarını analiz et ve nedenlerini karşılaştır.", "analysis")]
    [InlineData("Extract the invoice number from this text.", "classification")]
    [InlineData("Tell me about the latest payment.", null)]
    public void Classifies_task_hints_from_user_prompts(string prompt, string? expected)
    {
        Assert.Equal(expected, TaskTypeClassifier.Classify(prompt));
    }
}
