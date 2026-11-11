using Microsoft.EntityFrameworkCore;
using PRI.Domain.AiGateway.Entities;
using PRI.Domain.Common;
using PRI.Domain.Revenue.Entities;

namespace PRI.Infrastructure.Persistence.Context;

/// <summary>Tek DbContext. Modüller kendi tablolarını ayrı şemalarda ve ayrı konfigürasyon dosyalarında tanımlar.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    // Revenue modülü
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();

    // AI Gateway modülü
    public DbSet<GatewayClient> GatewayClients => Set<GatewayClient>();

    public DbSet<ProviderModel> ProviderModels => Set<ProviderModel>();

    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();

    public DbSet<SemanticCacheEntry> SemanticCacheEntries => Set<SemanticCacheEntry>();

    public DbSet<CacheLookup> CacheLookups => Set<CacheLookup>();

    public DbSet<AiBudget> AiBudgets => Set<AiBudget>();

    public DbSet<RevenueIssue> RevenueIssues => Set<RevenueIssue>();

    public DbSet<RecoveryAction> RecoveryActions => Set<RecoveryAction>();

    public DbSet<AiInsight> AiInsights => Set<AiInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
