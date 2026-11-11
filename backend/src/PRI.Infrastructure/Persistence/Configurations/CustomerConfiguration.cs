using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", "revenue");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DataSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(c => c.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => new { c.DataSource, c.ExternalId }).IsUnique();
        builder.HasIndex(c => c.Email);
    }
}
