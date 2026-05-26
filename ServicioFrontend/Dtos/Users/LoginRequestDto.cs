namespace ServicioFrontend.Dtos.Users;

public sealed record LoginRequestDto(
    string EmailOrUsername,
    string Password);
