using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> builder)
    {
        builder.ToTable("routing_rules", "gateway");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TaskType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.FallbackModelIds).HasColumnType("uuid[]").IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasIndex(r => r.TaskType).IsUnique();

        builder.HasOne<ProviderModel>()
            .WithMany()
            .HasForeignKey(r => r.PrimaryModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
