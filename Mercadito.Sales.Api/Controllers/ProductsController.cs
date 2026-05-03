using System.Security.Claims;
using Mercadito.Sales.Api.Contracts.Categories;
using Mercadito.Sales.Api.Contracts.Common;
using Mercadito.Sales.Api.Contracts.Products;
using Mercadito.src.application.categories.models;
using Mercadito.src.application.products.models;
using Mercadito.src.application.products.ports.input;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.domain.shared;
using Microsoft.AspNetCore.Mvc;

namespace Mercadito.Sales.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductManagementUseCase productManagementUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProductPageResponse>>> GetPageAsync(
        [FromQuery] long categoryFilter = 0,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] long anchorProductId = 0,
        [FromQuery] long cursorProductId = 0,
        [FromQuery] bool isNextPage = true,
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 50);
        var effectiveCategoryFilter = Math.Max(0, categoryFilter);
        IReadOnlyList<ProductWithCategoriesModel> products = cursorProductId > 0
            ? await productManagementUseCase.GetPageByCursorAsync(
                effectiveCategoryFilter,
                effectivePageSize,
                sortBy,
                sortDirection,
                cursorProductId,
                isNextPage,
                searchTerm,
                cancellationToken)
            : await productManagementUseCase.GetPageFromAnchorAsync(
                effectiveCategoryFilter,
                effectivePageSize,
                sortBy,
                sortDirection,
                anchorProductId,
                searchTerm,
                cancellationToken);

        var hasPreviousPage = false;
        var hasNextPage = false;
        if (products.Count > 0)
        {
            hasPreviousPage = await productManagementUseCase.HasProductsByCursorAsync(
                effectiveCategoryFilter,
                sortBy,
                sortDirection,
                products[0].Id,
                isNextPage: false,
                searchTerm,
                cancellationToken);

            hasNextPage = await productManagementUseCase.HasProductsByCursorAsync(
                effectiveCategoryFilter,
                sortBy,
                sortDirection,
                products[^1].Id,
                isNextPage: true,
                searchTerm,
                cancellationToken);
        }

        var categories = await productManagementUseCase.GetCategoriesAsync(cancellationToken);
        return Ok(ApiResponse<ProductPageResponse>.Ok(new ProductPageResponse(
            MapProducts(products),
            MapCategories(categories),
            hasPreviousPage,
            hasNextPage)));
    }

    [HttpGet("{productId:long}")]
    public async Task<ActionResult<ApiResponse<ProductForEditResponse>>> GetByIdAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        var product = await productManagementUseCase.GetForEditAsync(productId, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<ProductForEditResponse>.Fail("Producto no encontrado."));
        }

        return Ok(ApiResponse<ProductForEditResponse>.Ok(new ProductForEditResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Stock ?? 0,
            product.Batch,
            product.ExpirationDate,
            product.Price ?? 0m,
            product.CategoryIds.ToList())));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var createProduct = new CreateProductDto
        {
            Name = request.Name,
            Description = request.Description,
            Stock = request.Stock,
            Batch = request.Batch,
            ExpirationDate = request.ExpirationDate,
            Price = request.Price
        };

        foreach (var categoryId in request.CategoryIds)
        {
            createProduct.CategoryIds.Add(categoryId);
        }

        var result = await productManagementUseCase.CreateAsync(createProduct, BuildActor(), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPut("{productId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAsync(
        long productId,
        SaveProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var updateProduct = new UpdateProductDto
        {
            Id = productId,
            Name = request.Name,
            Description = request.Description,
            Stock = request.Stock,
            Batch = request.Batch,
            ExpirationDate = request.ExpirationDate,
            Price = request.Price
        };

        foreach (var categoryId in request.CategoryIds)
        {
            updateProduct.CategoryIds.Add(categoryId);
        }

        var result = await productManagementUseCase.UpdateAsync(updateProduct, BuildActor(), cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(ToFailure<bool>(result));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{productId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long productId,
        CancellationToken cancellationToken = default)
    {
        var wasDeleted = await productManagementUseCase.DeleteAsync(productId, BuildActor(), cancellationToken);
        if (!wasDeleted)
        {
            return NotFound(ApiResponse<bool>.Fail("El producto no existe o ya estaba desactivado."));
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    private static IReadOnlyList<ProductResponse> MapProducts(IReadOnlyList<ProductWithCategoriesModel> products)
    {
        return products
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Stock,
                product.Batch,
                product.ExpirationDate,
                product.Price,
                product.Categories.ToList()))
            .ToList();
    }

    private static IReadOnlyList<CategoryResponse> MapCategories(IReadOnlyList<CategoryModel> categories)
    {
        return categories
            .Select(category => new CategoryResponse(
                category.Id,
                category.Code,
                category.Name,
                category.Description,
                category.ProductCount))
            .ToList();
    }

    private AuditActor BuildActor()
    {
        return new AuditActor
        {
            UserId = ResolveUserId(),
            Username = ResolveUsername(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
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

    private string ResolveUsername()
    {
        if (!string.IsNullOrWhiteSpace(User.Identity?.Name))
        {
            return User.Identity.Name;
        }

        var username = Request.Headers["X-Username"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        return "frontend";
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
