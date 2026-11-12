using Microsoft.EntityFrameworkCore;
using PRI.Domain.Common;
using PRI.Domain.Common.Interfaces;
using PRI.Infrastructure.Persistence.Context;

namespace PRI.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

    public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
        db.Users.CountAsync(u => u.Role == role, cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
