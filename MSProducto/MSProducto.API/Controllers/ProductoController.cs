namespace MSProducto.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using MSProducto.Application.DTOs;
using MSProducto.Application.Mappers;
using MSProducto.Domain.Common;
using MSProducto.Domain.Entities;
using MSProducto.Domain.Ports.Input;

[Route("api/products")]
[ApiController]
public class ProductoController : ControllerBase
{
    private readonly IProductoService _service;

    public ProductoController(IProductoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductoDto>>> GetAll(
        [FromQuery] long categoryFilter = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "nombre",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] long cursorProductId = 0,
        [FromQuery] bool isNextPage = true,
        [FromQuery] string searchTerm = "",
        CancellationToken ct = default)
    {
        var productos = await _service.GetPageByCursorAsync(
            categoryFilter, pageSize, sortBy, sortDirection, cursorProductId, isNextPage, searchTerm, ct);
        return Ok(productos.Select(ProductoMapper.ToDto));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductoDto>> Get(long id, CancellationToken ct = default)
    {
        var producto = await _service.GetForEditAsync(id, ct);
        if (producto == null)
            return NotFound(new { error = "Producto no encontrado." });
        return Ok(ProductoMapper.ToDto(producto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductoDto dto, CancellationToken ct = default)
    {
        var producto = ProductoMapper.ToDomain(dto);
        var result = await _service.CreateAsync(producto, null!, ct);
        return MapResult(result, 201);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductoDto dto, CancellationToken ct = default)
    {
        var producto = await _service.GetForEditAsync(id, ct);
        if (producto == null)
            return NotFound(new { error = "Producto no encontrado." });

        producto.Name = dto.Nombre;
        producto.Description = dto.Descripcion;
        producto.Batch = dto.Lote;
        producto.ExpirationDate = dto.FechaCaducidad;
        producto.Price = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoryIds = dto.CategoriaIds;

        var result = await _service.UpdateAsync(producto, null!, ct);
        return MapResult(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteAsync(id, null!, ct);
        if (!deleted)
            return NotFound(new { error = "Producto no encontrado." });
        return NoContent();
    }

    private IActionResult MapResult(Result r, int successCode = 200)
    {
        if (r.IsSuccess) return StatusCode(successCode);
        if (r.ValidationErrors?.Any() == true)
            return UnprocessableEntity(new { errors = r.ValidationErrors });
        if (r.Error?.Contains("Acceso denegado") == true)
            return Unauthorized(new { error = r.Error });
        if (r.Error?.Contains("no encontrado") == true || r.Error?.Contains("not found") == true)
            return NotFound(new { error = r.Error });
        return BadRequest(new { error = r.Error });
    }
}