namespace ServicioFrontend.Dtos.Users;

public sealed record LoginResponseDto(
    string AccessToken,
    bool NeedChangePassword);
