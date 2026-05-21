using System.Linq;
using Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Common;

public static class ApiResponseMapper
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result is null) return controller.StatusCode(StatusCodes.Status500InternalServerError);

        if (result.IsSuccess)
        {
            var response = new ApiResponse<T>(true, result.Value, null, null);
            return controller.StatusCode(successStatusCode, response);
        }

        var errors = result.Errors ?? System.Array.Empty<AppError>();
        var apiResponse = new ApiResponse<T>(false, default, errors, null);
        var primary = errors.FirstOrDefault();

        return primary?.Type switch
        {
            ErrorType.Validation => controller.UnprocessableEntity(apiResponse),
            ErrorType.NotFound => controller.NotFound(apiResponse),
            ErrorType.Conflict => controller.Conflict(apiResponse),
            ErrorType.NotImplemented => controller.StatusCode(StatusCodes.Status501NotImplemented, apiResponse),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, apiResponse)
        };
    }

    public static IActionResult ToActionResult(this ControllerBase controller, Result result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result is null) return controller.StatusCode(StatusCodes.Status500InternalServerError);

        if (result.IsSuccess)
        {
            var response = new ApiResponse<object?>(true, null, null, null);
            return controller.StatusCode(successStatusCode, response);
        }

        var errors = result.Errors ?? System.Array.Empty<AppError>();
        var apiResponse = new ApiResponse<object?>(false, null, errors, null);
        var primary = errors.FirstOrDefault();

        return primary?.Type switch
        {
            ErrorType.Validation => controller.UnprocessableEntity(apiResponse),
            ErrorType.NotFound => controller.NotFound(apiResponse),
            ErrorType.Conflict => controller.Conflict(apiResponse),
            ErrorType.NotImplemented => controller.StatusCode(StatusCodes.Status501NotImplemented, apiResponse),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, apiResponse)
        };
    }
}
