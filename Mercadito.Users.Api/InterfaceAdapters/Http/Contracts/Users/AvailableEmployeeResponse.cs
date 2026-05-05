namespace Mercadito.Users.Api.InterfaceAdapters.Http.Contracts.Users;

public sealed record AvailableEmployeeResponse(
    long Id,
    string FullName,
    string Cargo,
    string CiDisplay);
