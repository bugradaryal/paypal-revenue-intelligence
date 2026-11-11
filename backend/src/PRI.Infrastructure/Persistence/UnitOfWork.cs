using Microsoft.EntityFrameworkCore;
using PRI.Domain.Common.Interfaces;
using PRI.Infrastructure.Persistence.Context;
using PRI.Shared.Exceptions;

namespace PRI.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // İyimser eşzamanlılık (xmin): aynı kayıt başka bir istekle değiştirildi (ör. öneriye çift tıklama).
            throw new ConflictException("The record was changed by another request. Reload and try again.", "concurrent_update");
        }
    }
}
