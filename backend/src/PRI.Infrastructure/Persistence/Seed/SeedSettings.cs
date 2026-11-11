namespace PRI.Infrastructure.Persistence.Seed;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; init; } = string.Empty;

    public string AdminPassword { get; init; } = string.Empty;

    public string AdminDisplayName { get; init; } = "Admin";

    /// <summary>
    /// true ise Development ortamında demo kayıtları oluşturulur ve raporlara dahil edilir.
    /// false iken raporlar yalnızca PayPal kaynaklı kayıtları içerir. Varsayılan kapalıdır.
    /// </summary>
    public bool DemoData { get; init; }
}
