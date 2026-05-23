namespace Mercadito.Frontend.Dtos.Users;

public sealed record LoginResponseDto(
    string AccessToken,
    bool NeedChangePassword);
