using PRI.Domain.Common;

namespace PRI.Application.Common.Interfaces;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken Generate(User user);
}
