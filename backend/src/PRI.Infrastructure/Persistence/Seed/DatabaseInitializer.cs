using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRI.Application.Common.Interfaces;
using PRI.Domain.Common;
using PRI.Infrastructure.AiGateway.Seed;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Seed;

/// <summary>Açılışta migration'ları uygular ve (ayar varsa) ilk admin kullanıcısını oluşturur.</summary>
public sealed class DatabaseInitializer(
    AppDbContext db,
    GatewaySeeder gatewaySeeder,
    DemoDataSeeder demoDataSeeder,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IOptions<SeedSettings> seedOptions,
    ILogger<DatabaseInitializer> logger)
{
    /// <param name="allowDemoData">Yalnızca Development'ta true verilir; Seed:DemoData ile birlikte demo veri yazılır.</param>
    public async Task InitializeAsync(bool allowDemoData, CancellationToken cancellationToken = default)
    {
        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No EF Core migrations found. Run 'Add-Migration InitialPlatform' first (see README).");
        }

        await db.Database.MigrateAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
        await gatewaySeeder.SeedAsync(cancellationToken);

        if (allowDemoData && seedOptions.Value.DemoData)
        {
            await demoDataSeeder.SeedAsync(cancellationToken);
        }
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var settings = seedOptions.Value;

        if (string.IsNullOrWhiteSpace(settings.AdminEmail) || string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            logger.LogInformation("Admin seed skipped: Seed:AdminEmail / Seed:AdminPassword are not configured.");
            return;
        }

        var email = settings.AdminEmail.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        var admin = User.Create(
            email,
            passwordHasher.Hash(settings.AdminPassword),
            settings.AdminDisplayName,
            UserRole.Admin,
            timeProvider.GetUtcNow().UtcDateTime);

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin user '{Email}' created.", email);
    }
}
