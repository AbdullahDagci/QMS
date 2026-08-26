namespace Qms.Contracts.Security;
public sealed record LoginRequest(string Email, string Password);
public sealed record QuickLoginRequest(string ProfileKey);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAtUtc, string ProfileKey, string DisplayName);
