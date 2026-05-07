namespace Mercadito.Frontend.Dtos.Users;

public sealed record PasswordResetTokenDto(
    long UserId,
    string UserName,
    string Email,
    DateTime ExpiresAtUtc);
