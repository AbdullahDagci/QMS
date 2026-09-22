namespace Qms.Contracts.Security;
public sealed record LoginRequest(string Email, string Password);
public sealed record QuickLoginRequest(string ProfileKey);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record LoginResponse(DateTimeOffset ExpiresAtUtc, string ProfileKey, string DisplayName);
