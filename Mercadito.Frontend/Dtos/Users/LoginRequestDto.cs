namespace Mercadito.Frontend.Dtos.Users;

public sealed record LoginRequestDto(
    string EmailOrUsername,
    string Password);
