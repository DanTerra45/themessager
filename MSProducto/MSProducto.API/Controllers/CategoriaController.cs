namespace MSProducto.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using MSProducto.Application.DTOs;
using MSProducto.Application.Mappers;
using MSProducto.Domain.Common;
using MSProducto.Domain.Entities;
using MSProducto.Domain.Ports.Input;

[Route("api/categoria")]
[ApiController]
public class CategoriaController : ControllerBase
{
    private readonly ICategoriaService _service;

    public CategoriaController(ICategoriaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> GetAll(CancellationToken ct = default)
    {
        var categorias = await _service.GetPageFromAnchorAsync(100, "nombre", "asc", 0, "", ct);
        return Ok(categorias.Select(CategoriaMapper.ToDto));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoriaDto>> Get(long id, CancellationToken ct = default)
    {
        var categoria = await _service.GetForEditAsync(id, ct);
        if (categoria == null)
            return NotFound(new { error = "Categoría no encontrada." });
        return Ok(CategoriaMapper.ToDto(categoria));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoriaDto dto, CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(CategoriaMapper.ToDomain(dto), null!, ct);
        if (result.IsSuccess)
        {
            var created = await _service.GetForEditAsync(result.Value, ct);
            if (created != null)
                return CreatedAtAction(nameof(Get), new { id = created.Id }, CategoriaMapper.ToDto(created));
            return CreatedAtAction(nameof(Get), new { id = result.Value }, result.Value);
        }
        return MapResult(result, 201);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCategoriaDto dto, CancellationToken ct = default)
    {
        var categoria = await _service.GetForEditAsync(id, ct);
        if (categoria == null)
            return NotFound(new { error = "Categoría no encontrada." });

        categoria.Name = dto.Nombre;
        categoria.Description = dto.Descripcion;

        var result = await _service.UpdateAsync(categoria, null!, ct);
        if (result.IsSuccess) return Ok();
        if (result.Error?.Contains("Acceso denegado") == true)
            return Unauthorized(new { error = result.Error });
        if (result.Error?.Contains("no encontrado") == true || result.Error?.Contains("not found") == true)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteAsync(id, null!, ct);
        if (!deleted)
            return NotFound(new { error = "Categoría no encontrada." });
        return NoContent();
    }

    private IActionResult MapResult<T>(Result<T> r, int successCode = 200)
    {
        if (r.IsSuccess) return StatusCode(successCode, r.Value);
        if (r.ValidationErrors?.Any() == true)
            return UnprocessableEntity(new { errors = r.ValidationErrors });
        if (r.Error?.Contains("Acceso denegado") == true)
            return Unauthorized(new { error = r.Error });
        if (r.Error?.Contains("no encontrado") == true || r.Error?.Contains("not found") == true)
            return NotFound(new { error = r.Error });
        return BadRequest(new { error = r.Error });
    }
}