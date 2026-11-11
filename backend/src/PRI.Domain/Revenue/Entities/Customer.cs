using PRI.Domain.Common;
using PRI.Domain.Revenue.Enums;

namespace PRI.Domain.Revenue.Entities;

public sealed class Customer : BaseEntity
{
    // EF Core için.
    private Customer()
    {
    }

    public DataSource DataSource { get; private set; }

    /// <summary>Kaynaktaki benzersiz kimlik (PayPal payer account id, yoksa e-posta tabanlı anahtar).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public static Customer Create(DataSource dataSource, string externalId, string name, string? email, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Customer
        {
            DataSource = dataSource,
            ExternalId = externalId.Trim(),
            Name = name.Trim(),
            Email = NormalizeEmail(email),
            CreatedAtUtc = nowUtc
        };
    }

    public void UpdateProfile(string name, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Email = NormalizeEmail(email) ?? Email;
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
