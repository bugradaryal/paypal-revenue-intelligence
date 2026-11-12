using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.ToTable("sync_runs", "revenue");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.StartedAtUtc).IsRequired();
        builder.Property(s => s.RangeFromUtc).IsRequired();
        builder.Property(s => s.RangeToUtc).IsRequired();
        builder.Property(s => s.InvoiceImportSkipped).IsRequired();
        builder.Property(s => s.Error).HasMaxLength(2000);
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.HasIndex(s => s.StartedAtUtc);
    }
}
