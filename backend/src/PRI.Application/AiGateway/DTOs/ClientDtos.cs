namespace PRI.Application.AiGateway.DTOs;

public sealed record CreateClientRequest(string Name);

public sealed record GatewayClientDto(
    Guid Id,
    string Name,
    bool IsFirstParty,
    bool IsActive,
    string? ApiKeyPrefix,
    DateTime CreatedAt);

/// <param name="ApiKey">Düz API anahtarı. Yalnızca bu yanıtta, bir kez gösterilir; sonradan geri alınamaz.</param>
public sealed record CreatedGatewayClientDto(
    Guid Id,
    string Name,
    string ApiKey,
    string ApiKeyPrefix,
    DateTime CreatedAt);
