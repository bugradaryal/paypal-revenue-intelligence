using PRI.Domain.Common;

namespace PRI.Application.Common.DTOs;

public sealed record UserDto(Guid Id, string Email, string DisplayName, UserRole Role, bool IsActive, bool IsPendingApproval);
