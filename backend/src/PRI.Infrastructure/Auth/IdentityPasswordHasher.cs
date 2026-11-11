using Microsoft.AspNetCore.Identity;
using PRI.Application.Common.Interfaces;
using PRI.Domain.Common;

namespace PRI.Infrastructure.Auth;

/// <summary>ASP.NET Core Identity'nin PBKDF2 tabanlı PasswordHasher'ını kullanır (tam Identity altyapısı gerekmez).</summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
