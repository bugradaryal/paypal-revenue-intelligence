namespace PRI.Application.Common.DTOs;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
