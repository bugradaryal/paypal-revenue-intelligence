using PRI.Infrastructure.Persistence.Seed;

namespace PRI.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Database:InitializeOnStartup açıksa migration uygular; admin, gateway kataloğu ve (Development'ta) demo veriyi seed eder.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        // Demo veri yalnızca Development'ta üretilir (Seed:DemoData ayrıca açık olmalıdır).
        await initializer.InitializeAsync(app.Environment.IsDevelopment());
    }
}
