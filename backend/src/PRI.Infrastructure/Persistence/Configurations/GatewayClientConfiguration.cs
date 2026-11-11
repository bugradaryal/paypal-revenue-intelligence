using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class GatewayClientConfiguration : IEntityTypeConfiguration<GatewayClient>
{
    public void Configure(EntityTypeBuilder<GatewayClient> builder)
    {
        builder.ToTable("clients", "gateway");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsFirstParty).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.ApiKeyHash).HasMaxLength(64);
        builder.Property(c => c.ApiKeyPrefix).HasMaxLength(20);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.ApiKeyHash).IsUnique();
    }
}
