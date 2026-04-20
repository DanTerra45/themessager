using Dapper;
using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared.exceptions;
using MySqlConnector;

namespace Mercadito.src.infrastructure.sales.persistence
{
    public sealed partial class SalesRepository
    {
        public async Task<string> GetNextSaleCodeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var currentYear = DateTime.Now.Year;
                int nextCodeNumber;

                try
                {
                    const string query = @"SELECT `nextValue`
                        FROM sale_code_sequence
                        WHERE `id` = @SequenceId";

                    var command = new CommandDefinition(
                        query,
                        new { SequenceId = SaleCodeSequenceId },
                        cancellationToken: cancellationToken);

                    var codeNumberFromSequence = await connection.ExecuteScalarAsync<int?>(command);
                    if (codeNumberFromSequence.HasValue && codeNumberFromSequence.Value > 0)
                    {
                        nextCodeNumber = codeNumberFromSequence.Value;
                    }
                    else
                    {
                        nextCodeNumber = await GetFallbackNextSaleCodeNumberAsync(connection, currentYear, null, cancellationToken);
                    }
                }
                catch (MySqlException exception) when (exception.Number == 1146)
                {
                    nextCodeNumber = await GetFallbackNextSaleCodeNumberAsync(connection, currentYear, null, cancellationToken);
                }

                if (nextCodeNumber > MaximumSaleCodeNumber)
                {
                    throw new BusinessValidationException("No hay más códigos de venta disponibles para la gestión actual.");
                }

                return FormatSaleCode(currentYear, nextCodeNumber);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el siguiente código de venta", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el siguiente código de venta", exception);
            }
        }

        public async Task<IReadOnlyList<CustomerLookupItem>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"
                    SELECT
                        c.id AS Id,
                        c.ciNit AS DocumentNumber,
                        c.razonSocial AS BusinessName
                    FROM clientes c
                    WHERE c.estado = @ActiveState
                    AND (
                        @SearchTerm = ''
                        OR c.ciNit LIKE @LikeSearchTerm
                        OR c.razonSocial LIKE @LikeSearchTerm
                    )
                    ORDER BY
                        CASE WHEN c.ciNit = '0' THEN 0 ELSE 1 END,
                        c.razonSocial ASC,
                        c.id ASC
                    LIMIT 20;";

                var command = new CommandDefinition(
                    query,
                    new
                    {
                        ActiveState,
                        SearchTerm = searchTerm,
                        LikeSearchTerm = BuildLikeValue(searchTerm)
                    },
                    cancellationToken: cancellationToken);

                var rows = await connection.QueryAsync<CustomerLookupRow>(command);
                var customers = new List<CustomerLookupItem>();
                foreach (var row in rows)
                {
                    customers.Add(new CustomerLookupItem(row.Id, row.DocumentNumber, row.BusinessName));
                }

                return customers;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar los clientes", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar los clientes", exception);
            }
        }

        public async Task<IReadOnlyList<SaleProductOption>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"
                    SELECT
                        p.id AS Id,
                        p.nombre AS Name,
                        p.lote AS Batch,
                        p.precio AS Price,
                        p.stock AS Stock
                    FROM products p
                    WHERE p.estado = @ActiveState
                    AND p.stock > 0
                    AND (
                        @SearchTerm = ''
                        OR p.nombre LIKE @LikeSearchTerm
                        OR p.lote LIKE @LikeSearchTerm
                    )
                    ORDER BY p.nombre ASC, p.lote ASC, p.id ASC
                    LIMIT 25;";

                var command = new CommandDefinition(
                    query,
                    new
                    {
                        ActiveState,
                        SearchTerm = searchTerm,
                        LikeSearchTerm = BuildLikeValue(searchTerm)
                    },
                    cancellationToken: cancellationToken);

                var rows = await connection.QueryAsync<SaleProductRow>(command);
                var products = new List<SaleProductOption>();
                foreach (var row in rows)
                {
                    products.Add(new SaleProductOption(row.Id, row.Name, row.Batch, row.Price, row.Stock));
                }

                return products;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar los productos para venta", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar los productos para venta", exception);
            }
        }

        public async Task<IReadOnlyList<SaleSummaryItem>> GetRecentSalesAsync(int take, string sortBy, string sortDirection, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                var orderByClause = BuildRecentSalesOrderByClause(sortBy, sortDirection);
                var query = string.Concat(@"
                    SELECT
                        v.id AS Id,
                        v.codigo AS Code,
                        v.fechaRegistro AS CreatedAt,
                        c.ciNit AS CustomerDocumentNumber,
                        c.razonSocial AS CustomerName,
                        v.canal AS Channel,
                        v.metodoPago AS PaymentMethod,
                        v.total AS Total,
                        v.estado AS Status
                    FROM ventas v
                    INNER JOIN clientes c ON c.id = v.clienteId
                    ORDER BY ", orderByClause, @"
                    LIMIT @Take;");

                var command = new CommandDefinition(
                    query,
                    new { Take = take },
                    cancellationToken: cancellationToken);

                var rows = await connection.QueryAsync<SaleSummaryRow>(command);
                var sales = new List<SaleSummaryItem>();
                foreach (var row in rows)
                {
                    sales.Add(new SaleSummaryItem(
                        row.Id,
                        row.Code,
                        row.CreatedAt,
                        row.CustomerDocumentNumber,
                        row.CustomerName,
                        row.Channel,
                        row.PaymentMethod,
                        row.Total,
                        row.Status));
                }

                return sales;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar las ventas recientes", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar las ventas recientes", exception);
            }
        }

        public async Task<SalesOverviewMetrics> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"
                    SELECT
                        SUM(CASE WHEN v.estado = @RegisteredStatus THEN 1 ELSE 0 END) AS RegisteredSalesCount,
                        SUM(CASE WHEN v.estado = @CancelledStatus THEN 1 ELSE 0 END) AS CancelledSalesCount,
                        COALESCE(SUM(CASE WHEN v.estado = @RegisteredStatus THEN v.total ELSE 0 END), 0) AS RegisteredAmountTotal,
                        COALESCE(SUM(CASE WHEN v.estado = @CancelledStatus THEN v.total ELSE 0 END), 0) AS CancelledAmountTotal,
                        SUM(CASE
                            WHEN v.estado = @RegisteredStatus
                            AND v.fechaRegistro >= CURRENT_DATE()
                            AND v.fechaRegistro < (CURRENT_DATE() + INTERVAL 1 DAY)
                            THEN 1
                            ELSE 0
                        END) AS SalesTodayCount,
                        COALESCE(SUM(CASE
                            WHEN v.estado = @RegisteredStatus
                            AND v.fechaRegistro >= CURRENT_DATE()
                            AND v.fechaRegistro < (CURRENT_DATE() + INTERVAL 1 DAY)
                            THEN v.total
                            ELSE 0
                        END), 0) AS SalesTodayTotal,
                        COALESCE(AVG(CASE
                            WHEN v.estado = @RegisteredStatus
                            AND v.fechaRegistro >= CURRENT_DATE()
                            AND v.fechaRegistro < (CURRENT_DATE() + INTERVAL 1 DAY)
                            THEN v.total
                            ELSE NULL
                        END), 0) AS AverageTicketToday
                    FROM ventas v;";

                var command = new CommandDefinition(
                    query,
                    new
                    {
                        RegisteredStatus,
                        CancelledStatus
                    },
                    cancellationToken: cancellationToken);

                var row = await connection.QuerySingleAsync<SalesOverviewMetricsRow>(command);
                return new SalesOverviewMetrics(
                    row.RegisteredSalesCount,
                    row.CancelledSalesCount,
                    row.RegisteredAmountTotal,
                    row.CancelledAmountTotal,
                    row.SalesTodayCount,
                    decimal.Round(row.SalesTodayTotal, 2, MidpointRounding.AwayFromZero),
                    decimal.Round(row.AverageTicketToday, 2, MidpointRounding.AwayFromZero));
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el resumen de ventas", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el resumen de ventas", exception);
            }
        }

        public async Task<SaleDetailDto?> GetSaleDetailAsync(long saleId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
                const string headerQuery = @"
                    SELECT
                        v.id AS Id,
                        v.codigo AS Code,
                        v.clienteId AS CustomerId,
                        c.ciNit AS CustomerDocumentNumber,
                        c.razonSocial AS CustomerName,
                        v.metodoPago AS PaymentMethod,
                        v.canal AS Channel,
                        v.estado AS Status,
                        v.usuarioUsername AS CreatedByUsername,
                        v.motivoAnulacion AS CancellationReason,
                        v.fechaRegistro AS CreatedAt,
                        v.fechaAnulacion AS CancelledAt,
                        v.total AS Total
                    FROM ventas v
                    INNER JOIN clientes c ON c.id = v.clienteId
                    WHERE v.id = @SaleId;";

                var headerCommand = new CommandDefinition(
                    headerQuery,
                    new { SaleId = saleId },
                    cancellationToken: cancellationToken);

                var header = await connection.QueryFirstOrDefaultAsync<SaleDetailHeaderRow>(headerCommand);
                if (header == null)
                {
                    return null;
                }

                const string linesQuery = @"
                    SELECT
                        d.productId AS ProductId,
                        d.nombreProductoSnapshot AS ProductName,
                        COALESCE(p.lote, '') AS Batch,
                        COALESCE(p.stock, 0) AS Stock,
                        d.cantidad AS Quantity,
                        d.precioUnitario AS UnitPrice,
                        d.importe AS Amount
                    FROM detalleVenta d
                    LEFT JOIN products p ON p.id = d.productId
                    WHERE d.ventaId = @SaleId
                    ORDER BY d.id ASC;";

                var linesCommand = new CommandDefinition(
                    linesQuery,
                    new { SaleId = saleId },
                    cancellationToken: cancellationToken);

                var lineRows = await connection.QueryAsync<SaleDetailLineRow>(linesCommand);
                var lines = new List<SaleDetailLineDto>();
                foreach (var lineRow in lineRows)
                {
                    lines.Add(new SaleDetailLineDto(
                        lineRow.ProductId,
                        lineRow.ProductName,
                        lineRow.Batch,
                        lineRow.Stock,
                        lineRow.Quantity,
                        lineRow.UnitPrice,
                        lineRow.Amount));
                }

                var cancellationReason = string.Empty;
                if (!string.IsNullOrWhiteSpace(header.CancellationReason))
                {
                    cancellationReason = header.CancellationReason;
                }

                return new SaleDetailDto
                {
                    Id = header.Id,
                    Code = header.Code,
                    CustomerId = header.CustomerId,
                    CustomerDocumentNumber = header.CustomerDocumentNumber,
                    CustomerName = header.CustomerName,
                    PaymentMethod = header.PaymentMethod,
                    Channel = header.Channel,
                    Status = header.Status,
                    CreatedByUsername = header.CreatedByUsername,
                    CancellationReason = cancellationReason,
                    CreatedAt = header.CreatedAt,
                    CancelledAt = header.CancelledAt,
                    Total = header.Total,
                    Lines = lines
                };
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el detalle de la venta", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el detalle de la venta", exception);
            }
        }

        public async Task<SaleReceiptDto?> GetSaleReceiptAsync(long saleId, CancellationToken cancellationToken = default)
        {
            var detail = await GetSaleDetailAsync(saleId, cancellationToken);
            if (detail == null)
            {
                return null;
            }

            var receiptLines = new List<SaleReceiptLineDto>(detail.Lines.Count);
            foreach (var line in detail.Lines)
            {
                receiptLines.Add(new SaleReceiptLineDto(
                    line.Quantity,
                    line.ProductName,
                    line.UnitPrice,
                    line.Amount));
            }

            return new SaleReceiptDto
            {
                Id = detail.Id,
                Code = detail.Code,
                CreatedAt = detail.CreatedAt,
                GeneratedAt = DateTime.Now,
                CustomerDocumentNumber = detail.CustomerDocumentNumber,
                CustomerName = detail.CustomerName,
                CreatedByUsername = detail.CreatedByUsername,
                Total = detail.Total,
                AmountInWords = BuildAmountInWords(detail.Total),
                Lines = receiptLines
            };
        }
    }
}
