using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PRI.Application.Revenue.Demo;
using PRI.Domain.Revenue.Enums;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Seed;

/// <summary>
/// Development'ta açıkça "demo" etiketli örnek veri yazar (müşteri, fatura, ödeme). Yalnızca hiç demo kaydı yoksa çalışır,
/// bu yüzden tekrar çalıştırmak veriyi çoğaltmaz. Demo verisini sıfırlamak için veritabanını silip yeniden oluşturun.
/// </summary>
public sealed class DemoDataSeeder(AppDbContext db, TimeProvider timeProvider, ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var exists = await db.Payments.AnyAsync(p => p.DataSource == DataSource.Demo, cancellationToken)
                     || await db.Customers.AnyAsync(c => c.DataSource == DataSource.Demo, cancellationToken);
        if (exists)
        {
            logger.LogInformation("Demo data already present; skipping.");

            return;
        }

        var data = DemoDataGenerator.Generate(timeProvider.GetUtcNow().UtcDateTime);

        db.Customers.AddRange(data.Customers);
        db.Invoices.AddRange(data.Invoices);
        db.Payments.AddRange(data.Payments);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo data created ({Customers} customers, {Invoices} invoices, {Payments} payments), all labeled dataSource=demo.",
            data.Customers.Count, data.Invoices.Count, data.Payments.Count);
    }
}
