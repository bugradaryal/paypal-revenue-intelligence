using PRI.Application.Common.DTOs;
using PRI.Application.Common.Interfaces;
using PRI.Domain.Common;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Application.Common.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAuthService
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return;
        }

        var user = User.Create(
            email,
            passwordHasher.Hash(request.Password),
            request.DisplayName,
            UserRole.Member,
            timeProvider.GetUtcNow().UtcDateTime,
            isActive: false,
            isPendingApproval: true);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken);

        // E-posta yok / şifre yanlış durumları aynı hatayı verir (hesap varlığı sızdırılmaz).
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            var code = user.IsPendingApproval ? "account_pending_approval" : "account_inactive";
            var message = user.IsPendingApproval
                ? "This account is waiting for administrator approval."
                : "This account has been deactivated.";
            throw new BusinessRuleException(code, message);
        }

        var token = tokenService.Generate(user);

        return new LoginResponse(token.Value, "Bearer", token.ExpiresAtUtc, ToDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("The user of this token no longer exists.");

        return ToDto(user);
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.IsActive, user.IsPendingApproval);
}
