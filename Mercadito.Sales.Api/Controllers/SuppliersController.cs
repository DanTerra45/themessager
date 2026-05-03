using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Contracts.Suppliers;
using Mercadito.src.domain.shared;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.validation;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Sales.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(
    IGetAllSuppliersUseCase getAllSuppliersUseCase,
    IGetSupplierByIdUseCase getSupplierByIdUseCase,
    IGetNextSupplierCodeUseCase getNextSupplierCodeUseCase,
    IRegisterSupplierUseCase registerSupplierUseCase,
    IUpdateSupplierUseCase updateSupplierUseCase,
    IDeleteSupplierUseCase deleteSupplierUseCase,
    ISupplierFormHintsProvider supplierFormHintsProvider) : ControllerBase
{
    private const string DefaultSortBy = "name";
    private const string DefaultSortDirection = "asc";

    [HttpGet]
    public async Task<ActionResult<ApiResponse<SupplierPageResponse>>> GetPageAsync(
        [FromQuery] string searchTerm = "",
        [FromQuery] string sortBy = DefaultSortBy,
        [FromQuery] string sortDirection = DefaultSortDirection,
        CancellationToken cancellationToken = default)
    {
        var suppliersResult = await getAllSuppliersUseCase.ExecuteAsync(cancellationToken);
        if (suppliersResult.IsFailure)
        {
            return BadRequest(ToFailure<SupplierPageResponse>(suppliersResult));
        }

        var suppliers = suppliersResult.Value;
        var normalizedSearchTerm = NormalizeText(searchTerm);
        if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
        {
            suppliers = suppliers
                .Where(supplier => MatchesSearch(supplier, normalizedSearchTerm))
                .ToList();
        }

        suppliers = SortSuppliers(
            suppliers,
            NormalizeSortBy(sortBy),
            NormalizeSortDirection(sortDirection));

        var nextCodeResult = await getNextSupplierCodeUseCase.ExecuteAsync(cancellationToken);
        var nextSupplierCode = nextCodeResult.IsSuccess && !string.IsNullOrWhiteSpace(nextCodeResult.Value)
            ? nextCodeResult.Value
            : "PRV001";

        return Ok(ApiResponse<SupplierPageResponse>.Ok(new SupplierPageResponse(
            MapSuppliers(suppliers),
            nextSupplierCode,
            supplierFormHintsProvider.GetHints())));
    }

    [HttpGet("{supplierId:long}")]
    public async Task<ActionResult<ApiResponse<SupplierResponse>>> GetByIdAsync(
        long supplierId,
        CancellationToken cancellationToken = default)
    {
        var result = await getSupplierByIdUseCase.ExecuteAsync(supplierId, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(ToFailure<SupplierResponse>(result));
        }

        return Ok(ApiResponse<SupplierResponse>.Ok(MapSupplier(result.Value)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateAsync(
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await registerSupplierUseCase.ExecuteAsync(new CreateSupplierDto
        {
            Codigo = request.Codigo,
            Nombre = request.Nombre,
            Direccion = request.Direccion,
            Contacto = request.Contacto,
            Rubro = request.Rubro,
            Telefono = request.Telefono
        }, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPut("{supplierId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAsync(
        long supplierId,
        SaveSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await updateSupplierUseCase.ExecuteAsync(new UpdateSupplierDto
        {
            Id = supplierId,
            Codigo = request.Codigo,
            Nombre = request.Nombre,
            Direccion = request.Direccion,
            Contacto = request.Contacto,
            Rubro = request.Rubro,
            Telefono = request.Telefono
        }, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{supplierId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long supplierId,
        CancellationToken cancellationToken = default)
    {
        var result = await deleteSupplierUseCase.ExecuteAsync(supplierId, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        if (result.Value == 0)
        {
            return NotFound(ApiResponse<bool>.Fail("El proveedor no existe o ya estaba desactivado."));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    private static IReadOnlyList<SupplierResponse> MapSuppliers(IReadOnlyList<SupplierDto> suppliers)
    {
        return suppliers.Select(MapSupplier).ToList();
    }

    private static SupplierResponse MapSupplier(SupplierDto supplier)
    {
        return new SupplierResponse(
            supplier.Id,
            supplier.Codigo,
            supplier.Nombre,
            supplier.Direccion,
            supplier.Contacto,
            supplier.Rubro,
            supplier.Telefono);
    }

    private static bool MatchesSearch(SupplierDto supplier, string searchTerm)
    {
        return ContainsIgnoreCase(supplier.Codigo, searchTerm)
            || ContainsIgnoreCase(supplier.Nombre, searchTerm)
            || ContainsIgnoreCase(supplier.Contacto, searchTerm)
            || ContainsIgnoreCase(supplier.Telefono, searchTerm)
            || ContainsIgnoreCase(supplier.Rubro, searchTerm);
    }

    private static bool ContainsIgnoreCase(string value, string searchTerm)
    {
        return value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<SupplierDto> SortSuppliers(
        IReadOnlyList<SupplierDto> suppliers,
        string sortBy,
        string sortDirection)
    {
        var orderedSuppliers = sortBy switch
        {
            "code" => OrderSuppliers(suppliers, supplier => supplier.Codigo, supplier => supplier.Id, sortDirection),
            "contact" => OrderSuppliers(suppliers, supplier => supplier.Contacto, supplier => supplier.Id, sortDirection),
            "phone" => OrderSuppliers(suppliers, supplier => supplier.Telefono, supplier => supplier.Id, sortDirection),
            "rubro" => OrderSuppliers(suppliers, supplier => supplier.Rubro, supplier => supplier.Id, sortDirection),
            _ => OrderSuppliers(suppliers, supplier => supplier.Nombre, supplier => supplier.Id, sortDirection)
        };

        return orderedSuppliers.ToList();
    }

    private static IOrderedEnumerable<SupplierDto> OrderSuppliers<TKey>(
        IEnumerable<SupplierDto> suppliers,
        Func<SupplierDto, TKey> primaryKeySelector,
        Func<SupplierDto, long> secondaryKeySelector,
        string sortDirection)
    {
        if (string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return suppliers.OrderByDescending(primaryKeySelector).ThenByDescending(secondaryKeySelector);
        }

        return suppliers.OrderBy(primaryKeySelector).ThenBy(secondaryKeySelector);
    }

    private static string NormalizeSortBy(string? value)
    {
        var normalizedValue = NormalizeText(value).ToLowerInvariant();
        return normalizedValue switch
        {
            "code" => "code",
            "contact" => "contact",
            "phone" => "phone",
            "rubro" => "rubro",
            _ => DefaultSortBy
        };
    }

    private static string NormalizeSortDirection(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : DefaultSortDirection;
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static ApiResponse<T> ToFailure<T>(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return ApiResponse<T>.Fail(result.Errors);
        }

        return ApiResponse<T>.Fail(result.ErrorMessage);
    }
}
