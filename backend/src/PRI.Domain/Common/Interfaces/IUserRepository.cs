namespace PRI.Domain.Common.Interfaces;

public interface IUserRepository
{
    /// <summary>E-posta küçük harfe çevrilmiş (normalize) olarak verilmelidir.</summary>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);

    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
