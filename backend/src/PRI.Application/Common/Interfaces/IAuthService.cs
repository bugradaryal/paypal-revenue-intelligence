using PRI.Application.Common.DTOs;

namespace PRI.Application.Common.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
