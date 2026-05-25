namespace Mercadito.Frontend.Services;

using Mercadito.Frontend.Dtos.Categories;

public static class CategoriaMapper
{
    public static CategoryDto ToCategoryDto(MsCategoriaResponse ms) => new(
        Id: ms.Id,
        Code: ms.Codigo,
        Name: ms.Nombre,
        Description: ms.Descripcion ?? string.Empty,
        ProductCount: ms.ProductosActivosCount);

    public static MsCreateCategoriaRequest ToMsRequest(SaveCategoryRequestDto formDto)
    {
        var request = new MsCreateCategoriaRequest
        {
            Codigo = formDto.Code,
            Nombre = formDto.Name,
            Descripcion = formDto.Description
        };
        return request;
    }
}

public class MsCategoriaPageResponse
{
    public IReadOnlyList<MsCategoriaResponse> Categorias { get; set; } = new List<MsCategoriaResponse>();
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    public string SiguienteCodigo { get; set; } = string.Empty;
}

public class MsCategoriaResponse
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int ProductosActivosCount { get; set; }
    public int Estado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime UltimaActualizacion { get; set; }
}

public class MsCreateCategoriaRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}