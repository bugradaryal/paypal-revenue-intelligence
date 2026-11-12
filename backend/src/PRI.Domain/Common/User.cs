namespace PRI.Domain.Common;

/// <summary>Panele giriş yapan kullanıcı. Revenue ve AiGateway modülleri ortak kullanır.</summary>
public sealed class User : BaseEntity
{
    // EF Core için.
    private User()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsPendingApproval { get; private set; }

    public static User Create(
        string email,
        string passwordHash,
        string displayName,
        UserRole role,
        DateTime createdAtUtc,
        bool isActive = true,
        bool isPendingApproval = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            Role = role,
            IsActive = isActive,
            IsPendingApproval = isPendingApproval,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void Activate()
    {
        IsActive = true;
        IsPendingApproval = false;
    }

    public void Deactivate() => IsActive = false;
}
