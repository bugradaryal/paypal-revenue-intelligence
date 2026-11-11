namespace PRI.Application.Common.DTOs;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, UserDto User);
