namespace PRI.Domain.Common.Interfaces;

public interface IUserRepository
{
    /// <summary>E-posta küçük harfe çevrilmiş (normalize) olarak verilmelidir.</summary>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
