using ServicioProveedores.Domain.Suppliers.Entities;
using ServicioProveedores.Domain.Shared.Validation;

namespace ServicioProveedores.Domain.Suppliers.Factories
{
    public class SupplierFactory : ISupplierFactory
    {
        public Supplier CreateForInsert(CreateSupplierValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Supplier
            {
                RazonSocial = ValidationText.NormalizeCollapsed(input.Nombre),
                Direccion = ValidationText.NormalizeCollapsed(input.Direccion),
                Telefono = ValidationText.NormalizeTrimmed(input.Telefono),
                Contacto = ValidationText.NormalizeCollapsed(input.Contacto),
                Rubro = ValidationText.NormalizeCollapsed(input.Rubro),
                Codigo = ValidationText.NormalizeUpperTrimmed(input.Codigo)
            };
        }

        public Supplier CreateForUpdate(UpdateSupplierValues input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return new Supplier
            {
                Id = input.Id,
                RazonSocial = ValidationText.NormalizeCollapsed(input.Nombre),
                Direccion = ValidationText.NormalizeCollapsed(input.Direccion),
                Telefono = ValidationText.NormalizeTrimmed(input.Telefono),
                Contacto = ValidationText.NormalizeCollapsed(input.Contacto),
                Rubro = ValidationText.NormalizeCollapsed(input.Rubro),
                Codigo = ValidationText.NormalizeUpperTrimmed(input.Codigo)
            };
        }
    }
}

