using System.Security.Claims;
using ServicioCatalogo.Contracts.Categories;
using ServicioCatalogo.Contracts.Common;
using ServicioCatalogo.Application.Categories.Models;
using ServicioCatalogo.Application.Categories.Ports.Input;
using ServicioCatalogo.Domain.Audit.Entities;
using ServicioCatalogo.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ServicioCatalogo.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryManagementUseCase categoryManagementUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CategoryPageResponse>>> GetPageAsync(
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] long anchorCategoryId = 0,
        [FromQuery] long cursorCategoryId = 0,
        [FromQuery] bool isNextPage = true,
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 50);
        var categoriesResult = cursorCategoryId > 0
            ? await categoryManagementUseCase.GetPageByCursorAsync(
                effectivePageSize,
                sortBy,
                sortDirection,
                cursorCategoryId,
                isNextPage,
                searchTerm,
                cancellationToken)
            : await categoryManagementUseCase.GetPageFromAnchorAsync(
                effectivePageSize,
                sortBy,
                sortDirection,
                anchorCategoryId,
                searchTerm,
                cancellationToken);
        if (categoriesResult.IsFailure)
        {
            return ToFailureAction<CategoryPageResponse>(categoriesResult);
        }

        var categories = categoriesResult.Value;

        var hasPreviousPage = false;
        var hasNextPage = false;
        if (categories.Count > 0)
        {
            var hasPreviousResult = await categoryManagementUseCase.HasCategoriesByCursorAsync(
                sortBy,
                sortDirection,
                categories[0].Id,
                isNextPage: false,
                searchTerm,
                cancellationToken);
            if (hasPreviousResult.IsFailure)
            {
                return ToFailureAction<CategoryPageResponse>(hasPreviousResult);
            }
            hasPreviousPage = hasPreviousResult.Value;

            var hasNextResult = await categoryManagementUseCase.HasCategoriesByCursorAsync(
                sortBy,
                sortDirection,
                categories[^1].Id,
                isNextPage: true,
                searchTerm,
                cancellationToken);
            if (hasNextResult.IsFailure)
            {
                return ToFailureAction<CategoryPageResponse>(hasNextResult);
            }
            hasNextPage = hasNextResult.Value;
        }

        var nextCodeResult = await categoryManagementUseCase.GetNextCategoryCodePreviewAsync(cancellationToken);
        if (nextCodeResult.IsFailure)
        {
            return ToFailureAction<CategoryPageResponse>(nextCodeResult);
        }

        return Ok(ApiResponse<CategoryPageResponse>.Ok(new CategoryPageResponse(
            MapCategories(categories),
            hasPreviousPage,
            hasNextPage,
            nextCodeResult.Value)));
    }

    [HttpGet("{categoryId:long}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        var categoryResult = await categoryManagementUseCase.GetForEditAsync(categoryId, cancellationToken);
        if (categoryResult.IsFailure)
        {
            return ToFailureAction<CategoryResponse>(categoryResult);
        }
        var category = categoryResult.Value;

        return Ok(ApiResponse<CategoryResponse>.Ok(new CategoryResponse(
            category.Id,
            category.Code,
            category.Name,
            category.Description,
            ProductCount: 0)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> CreateAsync(
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryManagementUseCase.CreateAsync(
            new CreateCategoryDto
            {
                Name = request.Name,
                Description = request.Description,
                Code = request.Code
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPut("{categoryId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAsync(
        long categoryId,
        SaveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryManagementUseCase.UpdateAsync(
            new UpdateCategoryDto
            {
                Id = categoryId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description
            },
            BuildActor(),
            cancellationToken);

        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{categoryId:long}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await categoryManagementUseCase.DeleteAsync(categoryId, BuildActor(), cancellationToken);
        if (result.IsFailure)
        {
            return ToFailureAction<bool>(result);
        }

        return Ok(ApiResponse<bool>.Ok(true));
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
