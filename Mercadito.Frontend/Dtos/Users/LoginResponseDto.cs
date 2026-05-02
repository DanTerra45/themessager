namespace Mercadito.Frontend.Dtos.Users;

public sealed record LoginResponseDto(
    long UserId,
    string UserName,
    string Role,
    long? EmployeeId,
    bool MustChangePassword,
    DateTime? LastLoginAt);
