using Mercadito.src.employees.domain.entities;
using Mercadito.src.employees.application.models;

namespace Mercadito.src.employees.domain.factories
{
    public interface IEmployeeFactory
    {
        Employee CreateForInsert(CreateEmployeeDto dto);
        Employee CreateForUpdate(UpdateEmployeeDto dto);
    }
}

