using Mercadito.src.domain.employees.entities;

namespace Mercadito.src.domain.employees.factories
{
    public sealed record CreateEmployeeValues(
        long? Ci,
        string? Complemento,
        string Nombres,
        string PrimerApellido,
        string? SegundoApellido,
        string Cargo,
        string NumeroContacto);

    public sealed record UpdateEmployeeValues(
        long Id,
        long? Ci,
        string? Complemento,
        string Nombres,
        string PrimerApellido,
        string? SegundoApellido,
        string Cargo,
        string NumeroContacto);

    public interface IEmployeeFactory
    {
        Employee CreateForInsert(CreateEmployeeValues input);
        Employee CreateForUpdate(UpdateEmployeeValues input);
    }
}
