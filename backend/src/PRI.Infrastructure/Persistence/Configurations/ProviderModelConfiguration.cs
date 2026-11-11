using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class ProviderModelConfiguration : IEntityTypeConfiguration<ProviderModel>
{
    public void Configure(EntityTypeBuilder<ProviderModel> builder)
    {
        builder.ToTable("provider_models", "gateway");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(m => m.ModelId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Tier).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(m => m.InputPricePerMillionUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(m => m.CachedInputPricePerMillionUsd).HasPrecision(18, 6);
        builder.Property(m => m.OutputPricePerMillionUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(m => m.IsEnabled).IsRequired();
        builder.Property(m => m.IsBaseline).IsRequired();
        builder.Property(m => m.PricingSourceUrl).HasMaxLength(500).IsRequired();
        builder.Property(m => m.PricingCheckedOn).IsRequired();
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.HasIndex(m => new { m.Provider, m.ModelId }).IsUnique();
    }
}
