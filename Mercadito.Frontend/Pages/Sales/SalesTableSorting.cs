namespace Mercadito.Frontend.Pages.Sales;

internal static class SalesTableSorting
{
    internal const string DefaultSortBy = "createdat";
    internal const string DefaultSortDirection = "desc";

    internal static string NormalizeSortBy(string? value)
    {
        var normalizedValue = NormalizeLowerTrimmed(value);
        return normalizedValue switch
        {
            "code" or "createdat" or "customer" or "channel" or "paymentmethod" or "total" or "status" => normalizedValue,
            _ => DefaultSortBy
        };
    }

    internal static string NormalizeSortDirection(string? value)
    {
        var normalizedValue = NormalizeLowerTrimmed(value);
        if (string.Equals(normalizedValue, "asc", StringComparison.Ordinal))
        {
            return "asc";
        }

        return DefaultSortDirection;
    }

    internal static string GetSortIcon(string currentSortBy, string currentSortDirection, string columnName)
    {
        if (!string.Equals(currentSortBy, columnName, StringComparison.Ordinal))
        {
            return "bi-arrow-down-up";
        }

        if (string.Equals(currentSortDirection, "asc", StringComparison.Ordinal))
        {
            return "bi-sort-up";
        }

        return "bi-sort-down";
    }

    internal static string GetNextSortDirection(string currentSortBy, string currentSortDirection, string columnName)
    {
        if (!string.Equals(currentSortBy, columnName, StringComparison.Ordinal))
        {
            return "asc";
        }

        if (string.Equals(currentSortDirection, "asc", StringComparison.Ordinal))
        {
            return "desc";
        }

        return "asc";
    }

    private static string NormalizeLowerTrimmed(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
