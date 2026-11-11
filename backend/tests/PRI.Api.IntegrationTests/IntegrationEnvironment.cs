namespace PRI.Api.IntegrationTests;

/// <summary>
/// Entegrasyon testleri gerçek bir PostgreSQL gerektirir (docker compose up -d db). Varsayılan olarak ATLANIR; çalıştırmak için
/// PRI_RUN_INTEGRATION=1 ayarlayın. Test veritabanı her çalıştırmada silinip yeniden oluşturulur; geliştirme veritabanınıza dokunulmaz.
/// OpenAI ve PayPal çağrıları sahtedir: kota harcanmaz, internet gerekmez.
/// </summary>
internal static class IntegrationEnvironment
{
    public const string EnableVariable = "PRI_RUN_INTEGRATION";

    public const string ConnectionVariable = "PRI_TEST_CONNECTION";

    public static bool Enabled => Environment.GetEnvironmentVariable(EnableVariable) == "1";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionVariable)
        ?? "Host=localhost;Port=5432;Database=pri_integration_test;Username=pri;Password=change_me_local_only";
}

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (!IntegrationEnvironment.Enabled)
        {
            Skip = $"Integration tests are opt-in: start PostgreSQL (docker compose up -d db) and set {IntegrationEnvironment.EnableVariable}=1.";
        }
    }
}
