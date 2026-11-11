using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "revenue");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DataSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(p => p.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(p => p.FailureCode).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.InitiatedAtUtc).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
        builder.Property(p => p.SyncedAtUtc).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => new { p.DataSource, p.ExternalId }).IsUnique();
        builder.HasIndex(p => new { p.Status, p.InitiatedAtUtc });
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.InvoiceId);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Events)
            .WithOne()
            .HasForeignKey(e => e.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
