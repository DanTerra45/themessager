using System.Data;
using System.Text;
using Dapper;
using Mercadito.src.application.products.models;
using System.Text.Json;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.domain.shared.validation;

namespace Mercadito.src.infrastructure.products.persistence
{
    public partial class ProductRepository
    {
        private static DateOnly ToDateOnly(DateTime value)
        {
            return DateOnly.FromDateTime(value);
        }

        private static DateTime ToDateTime(DateOnly value)
        {
            return value.ToDateTime(TimeOnly.MinValue);
        }

        private static ProductWithCategoriesModel ToProductWithCategoriesModel((
            long Id,
            string Name,
            string Description,
            int Stock,
            string Batch,
            DateTime ExpirationDate,
            decimal Price,
            string CategoriesString) row)
        {
            return new ProductWithCategoriesModel
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                Stock = row.Stock,
                Batch = row.Batch,
                ExpirationDate = ToDateOnly(row.ExpirationDate),
                Price = row.Price,
                Categories = BuildCategoryNames(row.CategoriesString)
            };
        }

        private static IReadOnlyList<string> BuildCategoryNames(string categoriesString)
        {
            if (string.IsNullOrWhiteSpace(categoriesString))
            {
                return [];
            }

            return [.. categoriesString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }

        private static List<long> NormalizeCategoryIds(IReadOnlyList<long> categoryIds)
        {
            var normalizedCategoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var categoryId in categoryIds)
            {
                if (categoryId <= 0)
                {
                    continue;
                }

                if (uniqueCategoryIds.Add(categoryId))
                {
                    normalizedCategoryIds.Add(categoryId);
                }
            }

            return normalizedCategoryIds;
        }

        private static List<long> ParseCategoryIds(string categoryIdsString)
        {
            if (string.IsNullOrWhiteSpace(categoryIdsString))
            {
                return [];
            }

            var categoryIds = new List<long>();
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var rawCategoryId in categoryIdsString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!long.TryParse(rawCategoryId, out var categoryId) || categoryId <= 0)
                {
                    continue;
                }

                if (uniqueCategoryIds.Add(categoryId))
                {
                    categoryIds.Add(categoryId);
                }
            }

            return categoryIds;
        }

        private static CommandDefinition BuildInsertProductCategoriesCommand(
            long productId,
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var queryBuilder = new StringBuilder("INSERT INTO categoriaDeProducto (productId, categoriaId) VALUES ");
            var parameters = new DynamicParameters();
            parameters.Add("ProductId", productId);

            for (var index = 0; index < normalizedCategoryIds.Count; index++)
            {
                if (index > 0)
                {
                    queryBuilder.Append(", ");
                }

                var categoryParameterName = $"CategoryId{index}";
                queryBuilder.Append("(@ProductId, @")
                    .Append(categoryParameterName)
                    .Append(')');

                parameters.Add(categoryParameterName, normalizedCategoryIds[index]);
            }

            return new CommandDefinition(
                queryBuilder.ToString(),
                parameters: parameters,
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static CommandDefinition BuildCountActiveCategoriesCommand(
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT COUNT(*)
                    FROM JSON_TABLE(@CategoryIdsJson, '$[*]' COLUMNS (CategoryId BIGINT PATH '$')) requested
                    INNER JOIN categorias c ON c.id = requested.CategoryId
                    WHERE c.estado = @ActiveState";

            return new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    CategoryIdsJson = JsonSerializer.Serialize(normalizedCategoryIds)
                },
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static async Task EnsureAllCategoriesAreActiveAsync(
            IDbConnection connection,
            IReadOnlyList<long> normalizedCategoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (normalizedCategoryIds.Count == 0)
            {
                throw new BusinessValidationException("CategoryIds", "Debe seleccionar al menos una categoría activa.");
            }

            var activeCategoriesCommand = BuildCountActiveCategoriesCommand(
                normalizedCategoryIds,
                transaction,
                cancellationToken);

            var activeCategoriesCount = await connection.ExecuteScalarAsync<int>(activeCategoriesCommand);
            if (activeCategoriesCount != normalizedCategoryIds.Count)
            {
                throw new BusinessValidationException("CategoryIds", "Una o más categorías seleccionadas no existen o están inactivas.");
            }
        }

        private static CommandDefinition BuildRelatedCategoryIdsByProductCommand(
            long productId,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT DISTINCT categoriaId
                    FROM categoriaDeProducto
                    WHERE productId = @ProductId";

            return new CommandDefinition(
                query,
                parameters: new { ProductId = productId },
                transaction: transaction,
                cancellationToken: cancellationToken);
        }

        private static List<long> MergeCategoryIds(
            IReadOnlyList<long> firstCategoryIds,
            IReadOnlyList<long> secondCategoryIds)
        {
            var mergedCategoryIds = new List<long>(firstCategoryIds.Count + secondCategoryIds.Count);
            var uniqueCategoryIds = new HashSet<long>();

            foreach (var categoryId in firstCategoryIds)
            {
                if (categoryId <= 0 || !uniqueCategoryIds.Add(categoryId))
                {
                    continue;
                }

                mergedCategoryIds.Add(categoryId);
            }

            foreach (var categoryId in secondCategoryIds)
            {
                if (categoryId <= 0 || !uniqueCategoryIds.Add(categoryId))
                {
                    continue;
                }

                mergedCategoryIds.Add(categoryId);
            }

            return mergedCategoryIds;
        }

        private static async Task RecalculateCategoryProductCountsAsync(
            IDbConnection connection,
            IReadOnlyList<long> categoryIds,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            if (categoryIds.Count == 0)
            {
                return;
            }

            const string query = @"UPDATE categorias c
                    LEFT JOIN (
                        SELECT
                            cp.categoriaId AS CategoryId,
                            COUNT(DISTINCT cp.productId) AS ActiveProductCount
                        FROM categoriaDeProducto cp
                        INNER JOIN products p ON p.id = cp.productId
                        WHERE cp.categoriaId IN @CategoryIds
                        AND p.estado = @ActiveState
                        GROUP BY cp.categoriaId
                    ) counts ON counts.CategoryId = c.id
                    SET c.productosActivosCount = COALESCE(counts.ActiveProductCount, 0)
                    WHERE c.id IN @CategoryIds";

            var command = new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    CategoryIds = categoryIds
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static string BuildOrderByClause(string sortBy, string sortDirection, bool reverse = false)
        {
            var direction = NormalizeSortDirection(sortDirection);
            var effectiveDirection = direction;
            if (reverse)
            {
                if (string.Equals(direction, "ASC", StringComparison.Ordinal))
                {
                    effectiveDirection = "DESC";
                }
                else
                {
                    effectiveDirection = "ASC";
                }
            }

            var idTieDirection = "ASC";
            if (reverse)
            {
                idTieDirection = "DESC";
            }
            var normalizedSortBy = NormalizeSortBy(sortBy);

            return normalizedSortBy switch
            {
                "id" => $"p.id {effectiveDirection}",
                "stock" => $"p.stock {effectiveDirection}, p.id {idTieDirection}",
                "batch" => $"p.lote {effectiveDirection}, p.id {idTieDirection}",
                "expirationdate" => $"p.fechaCaducidad {effectiveDirection}, p.id {idTieDirection}",
                "price" => $"p.precio {effectiveDirection}, p.id {idTieDirection}",
                _ => $"p.nombre {effectiveDirection}, p.id {idTieDirection}"
            };
        }

        private static string NormalizeSortBy(string sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return "name";
            }

            var normalizedSortBy = ValidationText.NormalizeLowerTrimmed(sortBy);
            return normalizedSortBy switch
            {
                "id" => "id",
                "stock" => "stock",
                "batch" => "batch",
                "expirationdate" => "expirationdate",
                "price" => "price",
                _ => "name"
            };
        }

        private static string ResolveSortColumn(string normalizedSortBy)
        {
            return normalizedSortBy switch
            {
                "stock" => "stock",
                "batch" => "lote",
                "expirationdate" => "fechaCaducidad",
                "price" => "precio",
                _ => "nombre"
            };
        }

        private static string BuildKeysetComparator(string sortBy, string sortDirection, bool isNextPage)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var direction = NormalizeSortDirection(sortDirection);
            var isAscending = string.Equals(direction, "ASC", StringComparison.Ordinal);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                if (isNextPage)
                {
                    if (isAscending)
                    {
                        return "p.id > cursorProduct.id";
                    }

                    return "p.id < cursorProduct.id";
                }

                if (isAscending)
                {
                    return "p.id < cursorProduct.id";
                }

                return "p.id > cursorProduct.id";
            }

            var sortColumn = ResolveSortColumn(normalizedSortBy);
            var mainComparator = ">";
            if (isNextPage)
            {
                if (!isAscending)
                {
                    mainComparator = "<";
                }
            }
            else
            {
                if (isAscending)
                {
                    mainComparator = "<";
                }
                else
                {
                    mainComparator = ">";
                }
            }

            var tieComparator = "<";
            if (isNextPage)
            {
                tieComparator = ">";
            }

            return $@"(
                p.{sortColumn} {mainComparator} cursorProduct.{sortColumn}
                OR (p.{sortColumn} = cursorProduct.{sortColumn} AND p.id {tieComparator} cursorProduct.id)
            )";
        }

        private static string BuildAnchorInclusiveComparator(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeSortBy(sortBy);
            var direction = NormalizeSortDirection(sortDirection);
            var isAscending = string.Equals(direction, "ASC", StringComparison.Ordinal);

            if (string.Equals(normalizedSortBy, "id", StringComparison.Ordinal))
            {
                if (isAscending)
                {
                    return "p.id >= anchor.id";
                }

                return "p.id <= anchor.id";
            }

            var sortColumn = ResolveSortColumn(normalizedSortBy);
            var mainComparator = "<";
            if (isAscending)
            {
                mainComparator = ">";
            }
            const string tieComparator = ">=";

            return $@"(
                p.{sortColumn} {mainComparator} anchor.{sortColumn}
                OR (p.{sortColumn} = anchor.{sortColumn} AND p.id {tieComparator} anchor.id)
            )";
        }

        private static string NormalizeSortDirection(string sortDirection)
        {
            if (string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            {
                return "DESC";
            }

            return "ASC";
        }

        private static string BuildSearchPattern(string normalizedSearchTerm)
        {
            if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                return string.Empty;
            }

            var escapedSearchTerm = normalizedSearchTerm
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);

            return $"%{escapedSearchTerm}%";
        }

        private static string BuildFullTextSearchQuery(string normalizedSearchTerm)
        {
            if (string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                return string.Empty;
            }

            var terms = new List<string>();
            var rawTerms = normalizedSearchTerm.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawTerm in rawTerms)
            {
                var sanitizedBuilder = new StringBuilder(rawTerm.Length);
                foreach (var character in rawTerm)
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        sanitizedBuilder.Append(character);
                    }
                }

                var sanitizedTerm = sanitizedBuilder.ToString();
                if (sanitizedTerm.Length < 3)
                {
                    continue;
                }

                terms.Add($"+{sanitizedTerm}*");
            }

            if (terms.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(' ', terms);
        }

        private static void AppendSearchFilterByMatchedProductIds(StringBuilder queryBuilder, bool useFullTextSearch)
        {
            if (useFullTextSearch)
            {
                queryBuilder.Append(@" 
                AND p.id IN (
                    SELECT matched.ProductId
                    FROM (
                        SELECT p2.id AS ProductId
                        FROM products p2
                        WHERE p2.estado = @ActiveState
                        AND MATCH(p2.nombre) AGAINST (@SearchFullTextQuery IN BOOLEAN MODE)
                        UNION
                        SELECT pc2.productId AS ProductId
                        FROM categoriaDeProducto pc2
                        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                        WHERE c2.estado = @ActiveState
                        AND MATCH(c2.nombre) AGAINST (@SearchFullTextQuery IN BOOLEAN MODE)
                    ) matched
                )");
                return;
            }

            queryBuilder.Append(@" 
                AND p.id IN (
                    SELECT matched.ProductId
                    FROM (
                        SELECT p2.id AS ProductId
                        FROM products p2
                        WHERE p2.estado = @ActiveState
                        AND p2.nombre LIKE @SearchPattern ESCAPE '\\'
                        UNION
                        SELECT pc2.productId AS ProductId
                        FROM categoriaDeProducto pc2
                        INNER JOIN categorias c2 ON c2.id = pc2.categoriaId
                        WHERE c2.estado = @ActiveState
                        AND c2.nombre LIKE @SearchPattern ESCAPE '\\'
                    ) matched
                )");
        }

        private static CommandDefinition BuildProductsByIdsCommand(
            IReadOnlyList<long> productIds,
            CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT 
                    p.id as Id,
                    p.nombre as Name,
                    p.descripcion as Description,
                    p.stock as Stock,
                    p.lote as Batch,
                    p.fechaCaducidad as ExpirationDate,
                    p.precio as Price,
                    COALESCE(GROUP_CONCAT(DISTINCT c.nombre SEPARATOR ','), '') as CategoriesString
                FROM products p
                LEFT JOIN categoriaDeProducto pc ON p.id = pc.productId
                LEFT JOIN categorias c ON pc.categoriaId = c.id AND c.estado = @ActiveState
                WHERE p.estado = @ActiveState
                AND p.id IN @ProductIds
                GROUP BY p.id, p.nombre, p.descripcion, p.stock, p.lote, p.fechaCaducidad, p.precio";

            return new CommandDefinition(
                query,
                parameters: new
                {
                    ActiveState,
                    ProductIds = productIds
                },
                cancellationToken: cancellationToken);
        }

        private static List<ProductWithCategoriesModel> MapProductsWithCategoriesByIdOrder(
            IReadOnlyList<(
                long Id,
                string Name,
                string Description,
                int Stock,
                string Batch,
                DateTime ExpirationDate,
                decimal Price,
                string CategoriesString)> rows,
            IReadOnlyList<long> orderedProductIds)
        {
            var productsById = new Dictionary<long, ProductWithCategoriesModel>(rows.Count);
            foreach (var row in rows)
            {
                productsById[row.Id] = ToProductWithCategoriesModel(row);
            }

            var orderedProducts = new List<ProductWithCategoriesModel>(orderedProductIds.Count);
            foreach (var productId in orderedProductIds)
            {
                if (productsById.TryGetValue(productId, out var product))
                {
                    orderedProducts.Add(product);
                }
            }

            return orderedProducts;
        }
    }
}
