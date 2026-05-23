namespace Mercadito.Frontend.Dtos.Users;

public sealed record LoginRequestDto(
    string Email,
    string Password);
