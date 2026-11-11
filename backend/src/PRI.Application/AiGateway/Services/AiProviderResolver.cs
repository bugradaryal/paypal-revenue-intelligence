using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway.Enums;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

public sealed class AiProviderResolver(IEnumerable<IAiProvider> providers) : IAiProviderResolver
{
    public IAiProvider Get(AiProvider provider) =>
        providers.FirstOrDefault(p => p.Provider == provider)
        ?? throw new BusinessRuleException("provider_not_available", $"No adapter is registered for provider '{provider}'.");
}
