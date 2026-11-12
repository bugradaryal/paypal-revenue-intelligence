namespace PRI.Application.AiGateway.DTOs;

/// <param name="TaskType">Küçük harfli görev tipi; "default" eşleşmeyen istekler içindir.</param>
/// <param name="PrimaryModelId">Katalogdaki model kimliği (ör. "gpt-6-luna").</param>
/// <param name="FallbackModelIds">Auto yönlendirmede ana modelle birlikte görev uygunluğu ve fiyata göre sıralanan aday modeller.</param>
public sealed record RoutingRuleDto(string TaskType, string PrimaryModelId, IReadOnlyList<string> FallbackModelIds);

public sealed record SetRoutingRuleRequest(string TaskType, string PrimaryModelId, IReadOnlyList<string>? FallbackModelIds);
