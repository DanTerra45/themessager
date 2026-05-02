using System.Data;
using Dapper;
using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared.exceptions;
using Mercadito.src.domain.shared.validation;
using MySqlConnector;

namespace Mercadito.src.infrastructure.sales.persistence
{
    public sealed partial class SalesRepository
    {
        private static string BuildLikeValue(string searchTerm)
        {
            return $"%{searchTerm}%";
        }

        private static string FormatSaleCode(int year, int codeNumber)
        {
            return $"V-{year}-{codeNumber:00000}";
        }

        private static string BuildAmountInWords(decimal amount)
        {
            var roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            var integerPart = decimal.ToInt64(decimal.Truncate(roundedAmount));
            var decimalPart = decimal.ToInt32(decimal.Round((roundedAmount - integerPart) * 100m, 0, MidpointRounding.AwayFromZero));

            if (decimalPart == 100)
            {
                integerPart += 1;
                decimalPart = 0;
            }

            var integerPartInWords = ConvertNumberToWords(integerPart);
            return string.Concat(integerPartInWords, " ", decimalPart.ToString("00"), "/100 Bolivianos");
        }

        private static string ConvertNumberToWords(long value)
        {
            if (value == 0)
            {
                return "Cero";
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var millions = value / 1000000;
            var thousands = (value % 1000000) / 1000;
            var hundreds = value % 1000;
            var parts = new List<string>();

            if (millions > 0)
            {
                if (millions == 1)
                {
                    parts.Add("Un Millon");
                }
                else
                {
                    parts.Add(string.Concat(ConvertHundredsToWords((int)millions), " Millones"));
                }
            }

            if (thousands > 0)
            {
                if (thousands == 1)
                {
                    parts.Add("Mil");
                }
                else
                {
                    parts.Add(string.Concat(ConvertHundredsToWords((int)thousands), " Mil"));
                }
            }

            if (hundreds > 0)
            {
                parts.Add(ConvertHundredsToWords((int)hundreds));
            }

            return string.Join(" ", parts);
        }

        private static string ConvertHundredsToWords(int value)
        {
            if (value == 0)
            {
                return string.Empty;
            }

            if (value == 100)
            {
                return "Cien";
            }

            var hundredsMap = new[]
            {
                string.Empty,
                "Ciento",
                "Doscientos",
                "Trescientos",
                "Cuatrocientos",
                "Quinientos",
                "Seiscientos",
                "Setecientos",
                "Ochocientos",
                "Novecientos"
            };

            var tensMap = new[]
            {
                string.Empty,
                "Diez",
                "Veinte",
                "Treinta",
                "Cuarenta",
                "Cincuenta",
                "Sesenta",
                "Setenta",
                "Ochenta",
                "Noventa"
            };

            var unitsMap = new[]
            {
                string.Empty,
                "Uno",
                "Dos",
                "Tres",
                "Cuatro",
                "Cinco",
                "Seis",
                "Siete",
                "Ocho",
                "Nueve"
            };

            var numbersBetweenTenAndTwentyNine = new Dictionary<int, string>
            {
                [10] = "Diez",
                [11] = "Once",
                [12] = "Doce",
                [13] = "Trece",
                [14] = "Catorce",
                [15] = "Quince",
                [16] = "Dieciseis",
                [17] = "Diecisiete",
                [18] = "Dieciocho",
                [19] = "Diecinueve",
                [20] = "Veinte",
                [21] = "Veintiuno",
                [22] = "Veintidos",
                [23] = "Veintitres",
                [24] = "Veinticuatro",
                [25] = "Veinticinco",
                [26] = "Veintiseis",
                [27] = "Veintisiete",
                [28] = "Veintiocho",
                [29] = "Veintinueve"
            };

            var parts = new List<string>();
            var hundreds = value / 100;
            var tensAndUnits = value % 100;

            if (hundreds > 0)
            {
                parts.Add(hundredsMap[hundreds]);
            }

            if (tensAndUnits == 0)
            {
                return string.Join(" ", parts);
            }

            if (numbersBetweenTenAndTwentyNine.TryGetValue(tensAndUnits, out var specialValue))
            {
                parts.Add(specialValue);
                return string.Join(" ", parts);
            }

            var tens = tensAndUnits / 10;
            var units = tensAndUnits % 10;

            if (tens > 0)
            {
                if (units == 0)
                {
                    parts.Add(tensMap[tens]);
                }
                else
                {
                    parts.Add(string.Concat(tensMap[tens], " y ", unitsMap[units]));
                }

                return string.Join(" ", parts);
            }

            parts.Add(unitsMap[units]);
            return string.Join(" ", parts);
        }

        private static async Task<int> GetFallbackNextSaleCodeNumberAsync(
            IDbConnection connection,
            int currentYear,
            IDbTransaction? transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT COALESCE(MAX(CAST(SUBSTRING(codigo, 8, 5) AS UNSIGNED)), 0) + 1
                FROM ventas
                WHERE codigo LIKE @CodePrefix;";

            var command = new CommandDefinition(
                query,
                new { CodePrefix = $"V-{currentYear}-%" },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var nextCodeNumber = await connection.ExecuteScalarAsync<int>(command);
            if (nextCodeNumber < 1)
            {
                return 1;
            }

            return nextCodeNumber;
        }

        private static async Task EnsureSaleCodeSequenceRowAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"
                INSERT INTO sale_code_sequence (`id`, `nextValue`)
                VALUES (@SequenceId, 1)
                ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`;";

            var command = new CommandDefinition(
                query,
                new { SequenceId = SaleCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        private static async Task<string> ReserveNextSaleCodeAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            int currentYear,
            CancellationToken cancellationToken)
        {
            await EnsureSaleCodeSequenceRowAsync(connection, transaction, cancellationToken);

            const string lockQuery = @"
                SELECT `nextValue`
                FROM sale_code_sequence
                WHERE `id` = @SequenceId
                FOR UPDATE;";

            var lockCommand = new CommandDefinition(
                lockQuery,
                new { SequenceId = SaleCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var reservedCodeNumber = await connection.ExecuteScalarAsync<int?>(lockCommand);
            var nextCodeNumber = 0;
            if (reservedCodeNumber.HasValue)
            {
                nextCodeNumber = reservedCodeNumber.Value;
            }

            if (nextCodeNumber <= 0)
            {
                nextCodeNumber = await GetFallbackNextSaleCodeNumberAsync(connection, currentYear, transaction, cancellationToken);
            }

            if (nextCodeNumber > MaximumSaleCodeNumber)
            {
                throw new BusinessValidationException("No hay más códigos de venta disponibles para la gestión actual.");
            }

            const string updateQuery = @"
                UPDATE sale_code_sequence
                SET `nextValue` = @FollowingCodeNumber
                WHERE `id` = @SequenceId;";

            var updateCommand = new CommandDefinition(
                updateQuery,
                new
                {
                    SequenceId = SaleCodeSequenceId,
                    FollowingCodeNumber = nextCodeNumber + 1
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(updateCommand);
            return FormatSaleCode(currentYear, nextCodeNumber);
        }

        private static async Task<Dictionary<long, ProductInventoryRow>> LoadProductsForSaleAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            IReadOnlyList<long> productIds,
            CancellationToken cancellationToken)
        {
            const string query = @"
                SELECT
                    p.id AS Id,
                    p.nombre AS Name,
                    p.precio AS Price,
                    p.stock AS Stock
                FROM products p
                WHERE p.id IN @ProductIds
                AND p.estado = @ActiveState
                FOR UPDATE;";

            var command = new CommandDefinition(
                query,
                new
                {
                    ProductIds = productIds,
                    ActiveState
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<ProductInventoryRow>(command);
            return rows.ToDictionary(row => row.Id);
        }

        private static void EnsureAllProductsAreAvailable(
            IReadOnlyDictionary<long, ProductInventoryRow> productsById,
            IReadOnlyList<RegisterSaleLineDto> lines,
            IReadOnlyDictionary<long, int>? stockCreditsByProductId = null)
        {
            foreach (var line in lines)
            {
                if (!productsById.TryGetValue(line.ProductId, out var product))
                {
                    throw new BusinessValidationException("Lines", "Uno de los productos seleccionados ya no está disponible.");
                }

                var availableStock = product.Stock;
                if (stockCreditsByProductId != null
                    && stockCreditsByProductId.TryGetValue(line.ProductId, out var creditedQuantity)
                    && creditedQuantity > 0)
                {
                    availableStock += creditedQuantity;
                }

                if (availableStock < line.Quantity)
                {
                    throw new BusinessValidationException("Lines", $"Stock insuficiente para {product.Name}.");
                }
            }
        }

        private static async Task<long> ResolveCustomerIdAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            long customerId,
            CreateCustomerDto newCustomer,
            string customerFieldName,
            CancellationToken cancellationToken)
        {
            if (customerId > 0)
            {
                const string customerQuery = @"
                    SELECT c.id AS Id
                    FROM clientes c
                    WHERE c.id = @CustomerId
                    AND c.estado = @ActiveState
                    LIMIT 1;";

                var customerCommand = new CommandDefinition(
                    customerQuery,
                    new
                    {
                        CustomerId = customerId,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var resolvedCustomerId = await connection.ExecuteScalarAsync<long?>(customerCommand);
                if (!resolvedCustomerId.HasValue || resolvedCustomerId.Value <= 0)
                {
                    throw new BusinessValidationException(customerFieldName, "El cliente seleccionado no está disponible.");
                }

                return resolvedCustomerId.Value;
            }

            const string existingCustomerQuery = @"
                SELECT
                    c.id AS Id,
                    c.ciNit AS DocumentNumber,
                    c.razonSocial AS BusinessName
                FROM clientes c
                WHERE c.ciNit = @DocumentNumber
                AND c.estado = @ActiveState
                LIMIT 1;";

            var existingCustomerCommand = new CommandDefinition(
                existingCustomerQuery,
                new
                {
                    DocumentNumber = newCustomer.DocumentNumber,
                    ActiveState
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var existingCustomer = await connection.QueryFirstOrDefaultAsync<CustomerLookupRow>(existingCustomerCommand);
            if (existingCustomer != null)
            {
                return existingCustomer.Id;
            }

            const string insertCustomerQuery = @"
                INSERT INTO clientes (
                    ciNit,
                    razonSocial,
                    telefono,
                    email,
                    direccion,
                    estado
                )
                VALUES (
                    @DocumentNumber,
                    @BusinessName,
                    @Phone,
                    @Email,
                    @Address,
                    @ActiveState
                );
                SELECT LAST_INSERT_ID();";

            var insertCustomerCommand = new CommandDefinition(
                insertCustomerQuery,
                new
                {
                    newCustomer.DocumentNumber,
                    newCustomer.BusinessName,
                    Phone = ToDbNullable(newCustomer.Phone),
                    Email = ToDbNullable(newCustomer.Email),
                    Address = ToDbNullable(newCustomer.Address),
                    ActiveState
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            return await connection.ExecuteScalarAsync<long>(insertCustomerCommand);
        }

        private static async Task<SaleStatusRow?> LoadSaleStatusAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            long saleId,
            CancellationToken cancellationToken)
        {
            const string saleQuery = @"
                SELECT
                    v.id AS Id,
                    v.estado AS Status
                FROM ventas v
                WHERE v.id = @SaleId
                FOR UPDATE;";

            var saleCommand = new CommandDefinition(
                saleQuery,
                new { SaleId = saleId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<SaleStatusRow>(saleCommand);
        }

        private static async Task<List<SaleQuantityRow>> LoadSaleLinesAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            long saleId,
            CancellationToken cancellationToken)
        {
            const string detailQuery = @"
                SELECT
                    d.productId AS ProductId,
                    d.cantidad AS Quantity
                FROM detalleVenta d
                WHERE d.ventaId = @SaleId;";

            var detailCommand = new CommandDefinition(
                detailQuery,
                new { SaleId = saleId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            return (await connection.QueryAsync<SaleQuantityRow>(detailCommand)).ToList();
        }

        private static string? ToDbNullable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        private static string BuildRecentSalesOrderByClause(string sortBy, string sortDirection)
        {
            var normalizedSortBy = NormalizeRecentSalesSortBy(sortBy);
            var normalizedSortDirection = NormalizeRecentSalesSortDirection(sortDirection);
            var directionSql = "DESC";
            if (string.Equals(normalizedSortDirection, "asc", StringComparison.Ordinal))
            {
                directionSql = "ASC";
            }

            var primaryColumn = "v.fechaRegistro";
            switch (normalizedSortBy)
            {
                case "code":
                    primaryColumn = "v.codigo";
                    break;
                case "customer":
                    primaryColumn = "c.razonSocial";
                    break;
                case "channel":
                    primaryColumn = "v.canal";
                    break;
                case "paymentmethod":
                    primaryColumn = "v.metodoPago";
                    break;
                case "total":
                    primaryColumn = "v.total";
                    break;
                case "status":
                    primaryColumn = "v.estado";
                    break;
            }

            return string.Concat(primaryColumn, " ", directionSql, ", v.id ", directionSql);
        }

        private static string NormalizeRecentSalesSortBy(string? value)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return "createdat";
            }

            switch (normalizedValue)
            {
                case "code":
                case "createdat":
                case "customer":
                case "channel":
                case "paymentmethod":
                case "total":
                case "status":
                    return normalizedValue;
                default:
                    return "createdat";
            }
        }

        private static string NormalizeRecentSalesSortDirection(string? value)
        {
            var normalizedValue = ValidationText.NormalizeLowerTrimmed(value);
            if (string.Equals(normalizedValue, "asc", StringComparison.Ordinal))
            {
                return "asc";
            }

            return "desc";
        }

        private sealed class CustomerLookupRow
        {
            public long Id { get; init; }
            public string DocumentNumber { get; init; } = string.Empty;
            public string BusinessName { get; init; } = string.Empty;
        }

        private sealed class SaleProductRow
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Batch { get; init; } = string.Empty;
            public decimal Price { get; init; }
            public int Stock { get; init; }
        }

        private sealed class ProductInventoryRow
        {
            public long Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public decimal Price { get; init; }
            public int Stock { get; init; }
        }

        private sealed class SaleLineInsertRow
        {
            public long ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal Amount { get; init; }
        }

        private sealed class SaleSummaryRow
        {
            public long Id { get; init; }
            public string Code { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
            public string CustomerDocumentNumber { get; init; } = string.Empty;
            public string CustomerName { get; init; } = string.Empty;
            public string Channel { get; init; } = string.Empty;
            public string PaymentMethod { get; init; } = string.Empty;
            public decimal Total { get; init; }
            public string Status { get; init; } = string.Empty;
        }

        private sealed class SalesOverviewMetricsRow
        {
            public int RegisteredSalesCount { get; init; }
            public int CancelledSalesCount { get; init; }
            public decimal RegisteredAmountTotal { get; init; }
            public decimal CancelledAmountTotal { get; init; }
            public int SalesTodayCount { get; init; }
            public decimal SalesTodayTotal { get; init; }
            public decimal AverageTicketToday { get; init; }
        }

        private sealed class SaleDetailHeaderRow
        {
            public long Id { get; init; }
            public string Code { get; init; } = string.Empty;
            public long CustomerId { get; init; }
            public string CustomerDocumentNumber { get; init; } = string.Empty;
            public string CustomerName { get; init; } = string.Empty;
            public string PaymentMethod { get; init; } = string.Empty;
            public string Channel { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public string CreatedByUsername { get; init; } = string.Empty;
            public string CancellationReason { get; init; } = string.Empty;
            public string CancelledByUsername { get; init; } = string.Empty;
            public DateTime CreatedAt { get; init; }
            public DateTime? CancelledAt { get; init; }
            public decimal Total { get; init; }
        }

        private sealed class SaleDetailLineRow
        {
            public long ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public string Batch { get; init; } = string.Empty;
            public int Stock { get; init; }
            public int Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal Amount { get; init; }
        }

        private sealed class SaleStatusRow
        {
            public long Id { get; init; }
            public string Status { get; init; } = string.Empty;
        }

        private sealed class SaleQuantityRow
        {
            public long ProductId { get; init; }
            public int Quantity { get; init; }
        }
    }
}
