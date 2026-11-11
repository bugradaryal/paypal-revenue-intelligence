using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class SemanticCacheEntryConfiguration : IEntityTypeConfiguration<SemanticCacheEntry>
{
    public void Configure(EntityTypeBuilder<SemanticCacheEntry> builder)
    {
        builder.ToTable("cache_entries", "gateway");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContextHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.QueryHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.NumbersFingerprint).HasMaxLength(64).IsRequired();
        // MVP: benzerlik C# tarafında hesaplanır; vektör düz real[] olarak saklanır (pgvector gerekmez).
        builder.Property(e => e.Embedding).HasColumnType("real[]");
        builder.Property(e => e.ResponseContent).IsRequired();
        builder.Property(e => e.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.ModelId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FinishReason).HasMaxLength(50);
        builder.Property(e => e.OriginalCostUsd).HasPrecision(18, 8).IsRequired();
        builder.Property(e => e.HitCount).IsRequired();
        builder.Property(e => e.ExpiresAtUtc).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        // Bilinçli olarak unique değil: eşzamanlı iki miss aynı kaydı yazabilir; arama en yeniyi kullanır.
        builder.HasIndex(e => new { e.ClientId, e.ContextHash, e.QueryHash });
        builder.HasIndex(e => new { e.ClientId, e.ContextHash, e.ExpiresAtUtc });

        builder.HasOne<GatewayClient>()
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
