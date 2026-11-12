using PRI.Domain.Common;

namespace PRI.Application.Common.DTOs;

public sealed record CreateUserRequest(string Email, string DisplayName, string Password, UserRole Role);
