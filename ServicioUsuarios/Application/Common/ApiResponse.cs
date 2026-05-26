using System.Collections.Generic;
using Domain.Common;

namespace Application.Common;

// Minimal API response envelope used by controllers
public sealed record ApiResponse<T>(
    bool Success,
    T? Data = default,
    IReadOnlyCollection<AppError>? Errors = null,
    object? Meta = null
);
