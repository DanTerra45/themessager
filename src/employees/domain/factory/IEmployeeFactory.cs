using Mercadito.src.employees.data.entity;
using Mercadito.src.employees.domain.dto;

namespace Mercadito.src.employees.domain.factory
{
    public interface IEmployeeFactory
    {
        Employee CreateForInsert(CreateEmployeeDto dto);
        Employee CreateForUpdate(UpdateEmployeeDto dto);
    }
}
