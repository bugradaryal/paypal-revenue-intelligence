using PRI.Application.Common.DTOs;

namespace PRI.Application.Common.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
