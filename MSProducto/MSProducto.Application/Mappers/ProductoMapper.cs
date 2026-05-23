namespace MSProducto.Application.Mappers
{
    using MSProducto.Domain.Entities;
    using MSProducto.Application.DTOs;

    public static class ProductoMapper
    {
        public static ProductoDto ToDto(Producto p)
        {
            return new ProductoDto
            {
                Id = p.Id,
                Nombre = p.Name,
                Descripcion = p.Description,
                Lote = p.Batch,
                FechaCaducidad = p.ExpirationDate,
                Precio = p.Price,
                Stock = p.Stock,
                Estado = true,
                ActivoUnico = false,
                FechaRegistro = DateTime.UtcNow,
                UltimaActualizacion = DateTime.UtcNow,
                Categorias = []
            };
        }

        public static Producto ToDomain(CreateProductoDto dto)
        {
            return new Producto
            {
                Name = dto.Nombre,
                Description = dto.Descripcion,
                Stock = dto.Stock,
                Batch = dto.Lote,
                ExpirationDate = dto.FechaCaducidad,
                Price = dto.Precio,
                CategoryIds = dto.CategoriaIds
            };
        }
    }
}