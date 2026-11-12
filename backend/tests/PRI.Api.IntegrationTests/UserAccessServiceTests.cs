using PRI.Application.Common.DTOs;
using PRI.Application.Common.Interfaces;
using PRI.Application.Common.Services;
using PRI.Domain.Common;
using PRI.Domain.Common.Interfaces;
using PRI.Shared.Exceptions;

namespace PRI.Api.IntegrationTests;

public sealed class UserAccessServiceTests
{
    [Fact]
    public async Task Admin_can_deactivate_another_user_but_not_themselves()
    {
        var admin = CreateUser("admin@example.test", UserRole.Admin);
        var member = CreateUser("member@example.test", UserRole.Member);
        var repository = new FakeUserRepository(admin, member);
        var unitOfWork = new FakeUnitOfWork();
        var service = new UserManagementService(repository, new FakePasswordHasher(), unitOfWork, TimeProvider.System);

        var result = await service.DeactivateAsync(member.Id, admin.Id, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.False(result.IsPendingApproval);
        Assert.Equal(1, unitOfWork.SaveCount);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.DeactivateAsync(admin.Id, admin.Id, CancellationToken.None));

        Assert.Equal("cannot_deactivate_own_account", exception.Code);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(false, true, "account_pending_approval")]
    [InlineData(false, false, "account_inactive")]
    public async Task Login_distinguishes_pending_registration_from_deactivated_account(
        bool isActive,
        bool isPendingApproval,
        string expectedCode)
    {
        var user = User.Create(
            "member@example.test",
            "password",
            "Member",
            UserRole.Member,
            DateTime.UtcNow,
            isActive,
            isPendingApproval);
        var service = new AuthService(
            new FakeUserRepository(user),
            new FakePasswordHasher(),
            new FakeTokenService(),
            new FakeUnitOfWork(),
            TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.LoginAsync(new LoginRequest(user.Email, "password"), CancellationToken.None));

        Assert.Equal(expectedCode, exception.Code);
    }

    private static User CreateUser(string email, UserRole role) =>
        User.Create(email, "password-hash", email, role, DateTime.UtcNow);

    private sealed class FakeUserRepository(params User[] initialUsers) : IUserRepository
    {
        private readonly List<User> users = [.. initialUsers];

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(users.FirstOrDefault(user => user.Email == normalizedEmail));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.FirstOrDefault(user => user.Id == id));

        public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>(users);

        public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.FirstOrDefault(user => user.Id == id));

        public Task<int> CountByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(users.Count(user => user.Role == role));

        public Task<int> CountActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(users.Count(user => user.Role == role && user.IsActive));

        public void Add(User user) => users.Add(user);

        public void Remove(User user) => users.Remove(user);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string passwordHash, string password) => passwordHash == password;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AccessToken Generate(User user) => new("token", DateTime.UtcNow.AddMinutes(15));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
