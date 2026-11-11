using FluentValidation;
using PRI.Application.AiGateway.DTOs;

namespace PRI.Application.AiGateway.Validators;

public sealed class CompletionRequestValidator : AbstractValidator<CompletionRequest>
{
    // Sağlayıcıların "uzun bağlam" (daha pahalı) fiyatı yüzbinlerce token'dan sonra başlar.
    // Toplam girdi bu sınırla, yani çok altında tutulur ki kayıtlı kısa bağlam fiyatları her zaman geçerli olsun.
    public const int MaxTotalCharacters = 400_000;

    private static readonly string[] ReasoningEfforts = ["none", "low", "medium", "high", "xhigh", "max"];

    public CompletionRequestValidator()
    {
        RuleFor(x => x.Model).MaximumLength(100);

        RuleFor(x => x.TaskType)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_-]+$").When(x => !string.IsNullOrEmpty(x.TaskType))
            .WithMessage("taskType may only contain letters, digits, '_' and '-'.");

        RuleFor(x => x.Messages)
            .NotNull().WithMessage("At least one message is required.")
            .Must(messages => messages is { Count: >= 1 and <= 50 })
            .WithMessage("Between 1 and 50 messages are required.");

        RuleForEach(x => x.Messages).SetValidator(new CompletionMessageValidator());

        RuleFor(x => x.Messages)
            .Must(messages => messages is null || messages.Sum(m => m.Content?.Length ?? 0) <= MaxTotalCharacters)
            .WithMessage($"The total message length must not exceed {MaxTotalCharacters} characters.");

        RuleFor(x => x.MaxOutputTokens).InclusiveBetween(1, 32_768).When(x => x.MaxOutputTokens is not null);

        RuleFor(x => x.Temperature).InclusiveBetween(0d, 2d).When(x => x.Temperature is not null);

        RuleFor(x => x.ReasoningEffort)
            .Must(value => value is null || ReasoningEfforts.Contains(value))
            .WithMessage("reasoningEffort must be one of: none, low, medium, high, xhigh, max.");
    }
}

internal sealed class CompletionMessageValidator : AbstractValidator<CompletionMessageDto>
{
    private static readonly string[] Roles = ["system", "user", "assistant"];

    public CompletionMessageValidator()
    {
        RuleFor(x => x.Role).Must(role => role is not null && Roles.Contains(role))
            .WithMessage("role must be one of: system, user, assistant.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}
