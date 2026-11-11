using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Configurations;

public sealed class RevenueIssueConfiguration : IEntityTypeConfiguration<RevenueIssue>
{
    public void Configure(EntityTypeBuilder<RevenueIssue> builder)
    {
        builder.ToTable("issues", "revenue");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.RecoveredAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(i => i.FailureCode).HasConversion<string>().HasMaxLength(32);
        builder.Property(i => i.DataSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(i => i.Evidence).HasConversion<string>().HasMaxLength(32);
        builder.Property(i => i.ProblemSinceUtc).IsRequired();
        builder.Property(i => i.UpdatedAtUtc).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        // PostgreSQL xmin ile iyimser eşzamanlılık: aynı kaydın iki istekle çakışan güncellenmesi 409 concurrent_update olur.
        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        // Aynı ödeme veya fatura için tür başına tek kayıp (NULL'lar unique'te ayrı sayıldığından iki kısmi indeks).
        builder.HasIndex(i => new { i.Type, i.PaymentId })
            .IsUnique()
            .HasFilter("\"PaymentId\" IS NOT NULL");
        builder.HasIndex(i => new { i.Type, i.InvoiceId })
            .IsUnique()
            .HasFilter("\"PaymentId\" IS NULL AND \"InvoiceId\" IS NOT NULL");
        builder.HasIndex(i => new { i.Status, i.ProblemSinceUtc });
        builder.HasIndex(i => i.ResolvedAtUtc);

        builder.HasOne<Customer>().WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(i => i.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(i => i.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RecoveryActionConfiguration : IEntityTypeConfiguration<RecoveryAction>
{
    public void Configure(EntityTypeBuilder<RecoveryAction> builder)
    {
        builder.ToTable("recovery_actions", "revenue");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(a => a.Rationale).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.RationaleLanguage).HasMaxLength(8).IsRequired();
        builder.Property(a => a.RationaleSource).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(a => a.ExpectedRecovery).HasPrecision(18, 2).IsRequired();
        builder.Property(a => a.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(a => a.DataSource).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(a => a.ExecutionError).HasMaxLength(100);
        builder.Property(a => a.UpdatedAtUtc).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasIndex(a => new { a.Status, a.CreatedAtUtc });
        builder.HasIndex(a => a.IssueId);
        builder.HasIndex(a => a.PaymentId);

        builder.HasOne<RevenueIssue>().WithMany().HasForeignKey(a => a.IssueId).OnDelete(DeleteBehavior.Cascade);

        // PaymentId/InvoiceId/CustomerId ve GatewayRequestId bilerek FK değildir: öneri geçmişi kaynak kayıtlardan ve
        // (başka modüle ait) gateway kayıtlarından bağımsız kalır.
    }
}

public sealed class AiInsightConfiguration : IEntityTypeConfiguration<AiInsight>
{
    public void Configure(EntityTypeBuilder<AiInsight> builder)
    {
        builder.ToTable("ai_insights", "revenue");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PeriodFromUtc).IsRequired();
        builder.Property(i => i.PeriodToUtc).IsRequired();
        builder.Property(i => i.MetricsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(i => i.Commentary).IsRequired();
        builder.Property(i => i.CommentaryLanguage).HasMaxLength(8).IsRequired();
        builder.Property(i => i.CommentarySource).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        builder.HasIndex(i => i.CreatedAtUtc);
    }
}
