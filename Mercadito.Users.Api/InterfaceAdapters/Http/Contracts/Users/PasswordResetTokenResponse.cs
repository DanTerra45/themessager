namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record PasswordResetTokenResponse(
    long UserId,
    string UserName,
    string Email,
    DateTime ExpiresAtUtc);
