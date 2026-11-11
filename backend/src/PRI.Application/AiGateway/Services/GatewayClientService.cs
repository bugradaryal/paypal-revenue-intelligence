using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Interfaces;
using PRI.Domain.AiGateway;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.AiGateway.Interfaces;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.AiGateway.Services;

public sealed class GatewayClientService(
    IGatewayClientRepository clients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IGatewayClientService
{
    public async Task<CreatedGatewayClientDto> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await clients.NameExistsAsync(name, cancellationToken))
        {
            throw new ConflictException($"A client named '{name}' already exists.");
        }

        var (plainKey, hash, prefix) = ApiKey.Generate();
        var client = GatewayClient.CreateThirdParty(name, hash, prefix, timeProvider.GetUtcNow().UtcDateTime);

        clients.Add(client);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Düz anahtar yalnızca burada, bir kez döner.
        return new CreatedGatewayClientDto(client.Id, client.Name, plainKey, prefix, client.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<GatewayClientDto>> ListAsync(CancellationToken cancellationToken)
    {
        var all = await clients.ListAsync(cancellationToken);

        return all
            .Select(c => new GatewayClientDto(c.Id, c.Name, c.IsFirstParty, c.IsActive, c.ApiKeyPrefix, c.CreatedAtUtc))
            .ToList();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await clients.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Client", id);

        if (client.IsFirstParty)
        {
            throw new BusinessRuleException("first_party_client_protected", "The first-party client cannot be deactivated.");
        }

        client.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
