using PRI.Application.Common.DTOs;

namespace PRI.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken);
}
