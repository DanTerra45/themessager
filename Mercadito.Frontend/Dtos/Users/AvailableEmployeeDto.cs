namespace Mercadito.Frontend.Dtos.Users;

public sealed record AvailableEmployeeDto(
    long Id,
    string FullName,
    string Cargo,
    string CiDisplay);
