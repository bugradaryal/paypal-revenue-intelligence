using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.AiGateway.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class AiBudgetConfiguration : IEntityTypeConfiguration<AiBudget>
{
    public void Configure(EntityTypeBuilder<AiBudget> builder)
    {
        builder.ToTable("budgets", "gateway");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.MonthlyLimitUsd).HasPrecision(18, 6).IsRequired();
        builder.Property(b => b.WarnAtPercent).IsRequired();
        builder.Property(b => b.EnforceLimit).IsRequired();
        builder.Property(b => b.UpdatedAtUtc).IsRequired();
        builder.Property(b => b.CreatedAtUtc).IsRequired();
    }
}
