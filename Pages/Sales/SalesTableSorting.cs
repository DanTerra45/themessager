using Mercadito.src.domain.shared.validation;

namespace Mercadito.Pages.Sales
{
    internal static class SalesTableSorting
    {
        internal const string DefaultSortBy = "createdat";
        internal const string DefaultSortDirection = "desc";

        internal static string NormalizeSortBy(string? value, params string[] allowedColumns)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
            foreach (var allowedColumn in allowedColumns)
            {
                if (string.Equals(normalizedValue, allowedColumn, StringComparison.Ordinal))
                {
                    return normalizedValue;
                }
            }

            return DefaultSortBy;
        }

        internal static string NormalizeSortDirection(string? value)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
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
    }
}
