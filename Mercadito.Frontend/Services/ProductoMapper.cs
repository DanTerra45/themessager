namespace Mercadito.Frontend.Services;

using Mercadito.Frontend.Dtos.Products;

public static class ProductoMapper
{
    public static ProductDto ToProductDto(MsProductoResponse ms) => new(
        Id: ms.Id,
        Name: ms.Nombre,
        Description: ms.Descripcion ?? string.Empty,
        Stock: ms.Stock,
        Batch: ms.Lote ?? string.Empty,
        ExpirationDate: ms.FechaCaducidad.HasValue ? DateOnly.FromDateTime(ms.FechaCaducidad.Value) : DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
        Price: ms.Precio,
        Categories: ms.Categorias ?? new List<string>());

    public static MsCreateProductoRequest ToMsRequest(SaveProductRequestDto formDto)
    {
        var request = new MsCreateProductoRequest
        {
            Nombre = formDto.Name,
            Descripcion = formDto.Description,
            Stock = formDto.Stock ?? 0,
            Lote = formDto.Batch,
            FechaCaducidad = formDto.ExpirationDate.ToDateTime(TimeOnly.MinValue),
            Precio = formDto.Price ?? 0.01m,
            Categorias = formDto.CategoryIds.Select(id => id.ToString()).ToList()
        };
        return request;
    }
}

public class MsProductoPageResponse
{
    public IReadOnlyList<MsProductoResponse> Productos { get; set; } = new List<MsProductoResponse>();
    public IReadOnlyList<MsCategoriaItemResponse> Categorias { get; set; } = new List<MsCategoriaItemResponse>();
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public class MsCategoriaItemResponse
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class MsProductoResponse
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Lote { get; set; }
    public DateTime? FechaCaducidad { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int Estado { get; set; }
    public bool ActivoUnico { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime UltimaActualizacion { get; set; }
    public IReadOnlyList<string>? Categorias { get; set; }
}

public class MsCreateProductoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Stock { get; set; }
    public string? Lote { get; set; }
    public DateTime? FechaCaducidad { get; set; }
    public decimal Precio { get; set; }
    public IReadOnlyList<string>? Categorias { get; set; }
}