using PRI.Application.Common.DTOs;
using PRI.Application.Common.Interfaces;
using PRI.Domain.Common;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Common.Services;

public sealed class UserManagementService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IUserManagementService
{
    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var usersList = await users.ListAsync(cancellationToken);
        return usersList.Select(ToDto).ToArray();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("A user with this email already exists.", "email_already_exists");
        }

        var user = User.Create(
            email,
            passwordHasher.Hash(request.Password),
            request.DisplayName,
            request.Role,
            timeProvider.GetUtcNow().UtcDateTime);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserDto> ActivateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetTrackedByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (!user.IsActive)
        {
            user.Activate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToDto(user);
    }

    public async Task<UserDto> DeactivateAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken)
    {
        if (userId == actingUserId)
        {
            throw new ConflictException("You cannot deactivate your own account.", "cannot_deactivate_own_account");
        }

        var user = await users.GetTrackedByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (user.IsActive
            && user.Role == UserRole.Admin
            && await users.CountActiveByRoleAsync(UserRole.Admin, cancellationToken) <= 1)
        {
            throw new ConflictException("The last active administrator account cannot be deactivated.", "last_active_admin_required");
        }

        if (user.IsActive)
        {
            user.Deactivate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToDto(user);
    }

    public async Task DeleteAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken)
    {
        if (userId == actingUserId)
        {
            throw new ConflictException("You cannot delete your own account.", "cannot_delete_own_account");
        }

        var user = await users.GetTrackedByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (user.Role == UserRole.Admin
            && await users.CountByRoleAsync(UserRole.Admin, cancellationToken) <= 1)
        {
            throw new ConflictException("The last administrator account cannot be deleted.", "last_admin_required");
        }

        users.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.IsActive, user.IsPendingApproval);
}
