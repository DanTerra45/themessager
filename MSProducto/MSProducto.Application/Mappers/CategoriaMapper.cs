namespace MSProducto.Application.Mappers
{
    using MSProducto.Domain.Entities;
    using MSProducto.Application.DTOs;

    public static class CategoriaMapper
    {
        public static CategoriaDto ToDto(Categoria c)
        {
            return new CategoriaDto
            {
                Id = c.Id,
                Codigo = c.Code,
                Nombre = c.Name,
                Descripcion = c.Description,
                ProductosActivosCount = 0,
                Estado = true,
                FechaRegistro = DateTime.UtcNow,
                UltimaActualizacion = DateTime.UtcNow
            };
        }

        public static Categoria ToDomain(CreateCategoriaDto dto)
        {
            return new Categoria
            {
                Code = dto.Codigo,
                Name = dto.Nombre,
                Description = dto.Descripcion
            };
        }
    }
}