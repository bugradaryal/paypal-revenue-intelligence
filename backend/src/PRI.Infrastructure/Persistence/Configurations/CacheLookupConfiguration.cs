using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class CacheLookupConfiguration : IEntityTypeConfiguration<CacheLookup>
{
    public void Configure(EntityTypeBuilder<CacheLookup> builder)
    {
        builder.ToTable("cache_lookups", "gateway");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Outcome).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(l => l.Similarity);
        builder.Property(l => l.EmbeddingTokens).IsRequired();
        builder.Property(l => l.EmbeddingCostUsd).HasPrecision(18, 8).IsRequired();
        builder.Property(l => l.SavedCostUsd).HasPrecision(18, 8).IsRequired();
        builder.Property(l => l.RequestedAtUtc).IsRequired();
        builder.Property(l => l.CreatedAtUtc).IsRequired();
        builder.Ignore(l => l.IsHit);

        builder.HasIndex(l => l.RequestedAtUtc);
        builder.HasIndex(l => new { l.ClientId, l.RequestedAtUtc });

        // CacheEntryId bilerek FK değildir: cache temizlense de geçmiş kayıtlar kalır.
        builder.HasOne<GatewayClient>()
            .WithMany()
            .HasForeignKey(l => l.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
