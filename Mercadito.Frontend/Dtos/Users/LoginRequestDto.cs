namespace Mercadito.Frontend.Dtos.Users;

public sealed record LoginRequestDto(
    string UserName,
    string Password);
