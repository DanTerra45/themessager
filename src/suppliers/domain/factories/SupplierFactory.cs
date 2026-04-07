using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.domain.entities;

namespace Mercadito.src.suppliers.domain.factories
{
    public class SupplierFactory : ISupplierFactory
    {
        public Supplier CreateForInsert(CreateSupplierDto dto)
        {
            return new Supplier
            {
                RazonSocial = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? string.Empty : dto.Telefono.Trim(),
                Contacto = dto.Contacto,
                Rubro = dto.Rubro,
                Codigo = dto.Codigo
            };
        }

        public Supplier CreateForUpdate(UpdateSupplierDto dto)
        {
            return new Supplier
            {
                Id = dto.Id,
                RazonSocial = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? string.Empty : dto.Telefono.Trim(),
                Contacto = dto.Contacto,
                Rubro = dto.Rubro,
                Codigo = dto.Codigo
            };
        }
    }
}
