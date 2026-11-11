using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events", "revenue");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalEventId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.ReceivedAtUtc).IsRequired();
        builder.Property(e => e.Outcome).HasMaxLength(100);
        builder.Property(e => e.CreatedAtUtc).IsRequired();

        builder.HasIndex(e => e.ExternalEventId).IsUnique();
    }
}
