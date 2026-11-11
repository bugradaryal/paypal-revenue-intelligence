using PRI.Application.AiGateway.DTOs;
using PRI.Application.AiGateway.Services;
using PRI.Domain.AiGateway;
using PRI.Domain.AiGateway.Entities;
using PRI.Shared.Exceptions;

namespace PRI.AiGateway.Tests;

public class GatewayClientTests
{
    [Fact]
    public void Generated_api_keys_have_a_prefix_and_a_matching_sha256_hash()
    {
        var (plain, hash, prefix) = ApiKey.Generate();

        Assert.StartsWith("pri_", plain);
        Assert.Equal(plain[..10], prefix);
        Assert.Equal(ApiKey.Hash(plain), hash);
        Assert.Equal(64, hash.Length);
        Assert.NotEqual(plain, hash);
    }

    [Fact]
    public void Each_generated_key_is_different()
    {
        Assert.NotEqual(ApiKey.Generate().PlainKey, ApiKey.Generate().PlainKey);
    }

    [Fact]
    public async Task Creating_a_client_returns_the_plain_key_once_and_stores_only_its_hash()
    {
        var repository = new FakeGatewayClientRepository();
        var service = new GatewayClientService(repository, new FakeUnitOfWork(), TimeProvider.System);

        var created = await service.CreateAsync(new CreateClientRequest("Acme"), CancellationToken.None);

        var stored = Assert.Single(repository.Clients);
        Assert.Equal(ApiKey.Hash(created.ApiKey), stored.ApiKeyHash);
        Assert.NotEqual(created.ApiKey, stored.ApiKeyHash);
        Assert.False(stored.IsFirstParty);

        var listed = Assert.Single(await service.ListAsync(CancellationToken.None));
        Assert.Equal(created.ApiKeyPrefix, listed.ApiKeyPrefix);
    }

    [Fact]
    public async Task Client_names_must_be_unique()
    {
        var repository = new FakeGatewayClientRepository();
        var service = new GatewayClientService(repository, new FakeUnitOfWork(), TimeProvider.System);
        await service.CreateAsync(new CreateClientRequest("Acme"), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(new CreateClientRequest("Acme"), CancellationToken.None));

        Assert.Equal("conflict", ex.Code);
    }

    [Fact]
    public async Task A_deactivated_client_can_no_longer_be_found_by_its_key()
    {
        var repository = new FakeGatewayClientRepository();
        var service = new GatewayClientService(repository, new FakeUnitOfWork(), TimeProvider.System);
        var created = await service.CreateAsync(new CreateClientRequest("Acme"), CancellationToken.None);

        await service.DeactivateAsync(created.Id, CancellationToken.None);

        Assert.Null(await repository.GetActiveByApiKeyHashAsync(ApiKey.Hash(created.ApiKey), CancellationToken.None));
    }

    [Fact]
    public async Task The_first_party_client_cannot_be_deactivated()
    {
        var repository = new FakeGatewayClientRepository();
        var firstParty = GatewayClient.CreateFirstParty("PRI Dashboard", TestData.Now);
        repository.Add(firstParty);
        var service = new GatewayClientService(repository, new FakeUnitOfWork(), TimeProvider.System);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.DeactivateAsync(firstParty.Id, CancellationToken.None));

        Assert.Equal("first_party_client_protected", ex.Code);
    }
}
