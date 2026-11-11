using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
{
    public void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        builder.ToTable("usage_records", "gateway");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(u => u.ModelId).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(u => u.CostUsd).HasPrecision(18, 8).IsRequired();
        builder.Property(u => u.TaskType).HasMaxLength(50);
        builder.Property(u => u.IsAutoRouted).IsRequired();
        builder.Property(u => u.Attempt).IsRequired();
        builder.Property(u => u.BaselineModelId).HasMaxLength(100);
        builder.Property(u => u.BaselineCostUsd).HasPrecision(18, 8);
        builder.Property(u => u.InputPricePerMillionUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(u => u.CachedInputPricePerMillionUsd).HasPrecision(18, 6);
        builder.Property(u => u.OutputPricePerMillionUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(u => u.ErrorCode).HasMaxLength(100);
        builder.Property(u => u.ProviderRequestId).HasMaxLength(100);
        builder.Property(u => u.PromptCharsSaved).HasDefaultValue(0).IsRequired();
        builder.Property(u => u.RequestedAtUtc).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.HasIndex(u => new { u.ClientId, u.RequestedAtUtc });
        builder.HasIndex(u => new { u.ProviderModelId, u.RequestedAtUtc });
        builder.HasIndex(u => u.RequestedAtUtc);

        builder.HasOne<GatewayClient>()
            .WithMany()
            .HasForeignKey(u => u.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProviderModel>()
            .WithMany()
            .HasForeignKey(u => u.ProviderModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
