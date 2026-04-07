using Mercadito.src.domain.provedores.dto;
using Mercadito.src.domain.provedores.model;

namespace Mercadito.src.domain.provedores.repository
{
    public class SupplierFactory : ISupplierFactory
    {
        public Supplier CreateForInsert(CreateSupplierDto dto)
        {
            return new Supplier()
            {
                RazonSocial = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Contacto = dto.Contacto,
                Rubro = dto.Rubro,
                Codigo = dto.Codigo
            };
        }
        public Supplier CreateForUpdate(UpdateSupplierDto dto)
        {
            return new Supplier()
            {
                Id = dto.Id,
                RazonSocial = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Contacto = dto.Contacto,
                Rubro = dto.Rubro,
                Codigo = dto.Codigo
            };
        }
    }
}