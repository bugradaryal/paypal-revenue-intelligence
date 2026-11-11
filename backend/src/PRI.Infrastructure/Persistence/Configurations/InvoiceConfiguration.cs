using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", "revenue");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.DataSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(i => i.InvoiceNumber).HasMaxLength(100);
        builder.Property(i => i.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.ProviderStatus).HasMaxLength(50).IsRequired();
        builder.Property(i => i.UpdatedAtUtc).IsRequired();
        builder.Property(i => i.SyncedAtUtc).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        builder.HasIndex(i => new { i.DataSource, i.ExternalId }).IsUnique();
        builder.HasIndex(i => i.InvoiceNumber);
        builder.HasIndex(i => new { i.Status, i.DueDate });

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
