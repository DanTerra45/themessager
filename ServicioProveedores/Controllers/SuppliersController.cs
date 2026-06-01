using ServicioProveedores.Contracts.Common;
using ServicioProveedores.Contracts.Suppliers;
using ServicioProveedores.Domain.Shared;
using ServicioProveedores.Application.Suppliers.Models;
using ServicioProveedores.Application.Suppliers.Ports.Input;
using ServicioProveedores.Application.Suppliers.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ServicioProveedores.Controllers;

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
            return ToFailureAction<SupplierPageResponse>(suppliersResult);
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
        if (nextCodeResult.IsFailure)
        {
            return ToFailureAction<SupplierPageResponse>(nextCodeResult);
        }

        var nextSupplierCode = nextCodeResult.Value;

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
            return ToFailureAction<SupplierResponse>(result);
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
        }, ResolveUserId(), cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
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
        }, ResolveUserId(), cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{supplierId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long supplierId,
        CancellationToken cancellationToken = default)
    {
        var result = await deleteSupplierUseCase.ExecuteAsync(supplierId, ResolveUserId(), cancellationToken);
        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
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

    private long ResolveUserId()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdText, out var userId) && userId > 0)
        {
            return userId;
        }

        userIdText = Request.Headers["X-User-Id"].FirstOrDefault();
        if (long.TryParse(userIdText, out userId) && userId > 0)
        {
            return userId;
        }

        return 1;
    }

    private static ApiResponse<T> ToFailure<T>(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return ApiResponse<T>.Fail(result.Errors);
        }

        return ApiResponse<T>.Fail(GetErrors(result).ToArray());
    }

    private static IReadOnlyList<string> GetErrors(Result result)
    {
        if (result.Errors.Count == 0)
        {
            return [result.ErrorMessage];
        }

        return result.Errors
            .SelectMany(error => error.Value)
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .DefaultIfEmpty(result.ErrorMessage)
            .ToList();
    }

    private static bool IsNotFoundFailure(Result result)
    {
        if (result.Errors.ContainsKey("NotFound"))
        {
            return true;
        }

        return result.ErrorMessage.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("no existe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidationFailure(Result result)
    {
        if (result.Errors.Count > 0)
        {
            return true;
        }

        return result.ErrorMessage.Contains("oblig", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("valid", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("invál", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("inval", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("formato", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("debe", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("requer", StringComparison.OrdinalIgnoreCase)
            || result.ErrorMessage.Contains("existe", StringComparison.OrdinalIgnoreCase);
    }

    private ActionResult<ApiResponse<T>> ToFailureAction<T>(Result result)
    {
        var payload = ToFailure<T>(result);
        if (IsNotFoundFailure(result))
        {
            return NotFound(payload);
        }

        if (IsValidationFailure(result))
        {
            return BadRequest(payload);
        }

        return StatusCode(StatusCodes.Status500InternalServerError, payload);
    }
}
