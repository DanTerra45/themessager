using System.Data;
using Dapper;
using Mercadito.src.domain.audit.entities;
using Mercadito.src.application.sales.models;
using Mercadito.src.domain.shared.exceptions;
using MySqlConnector;

namespace Mercadito.src.infrastructure.sales.persistence
{
    public sealed partial class SalesRepository
    {
        public async Task<long> RegisterAsync(RegisterSaleDto request, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(actor);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var currentYear = DateTime.Now.Year;
                var customerId = await ResolveCustomerIdAsync(connection, transaction, request, cancellationToken);
                var productIds = request.Lines.Select(line => line.ProductId).Distinct().ToList();
                var productsById = await LoadProductsForSaleAsync(connection, transaction, productIds, cancellationToken);
                EnsureAllProductsAreAvailable(productsById, request.Lines);

                decimal total = 0m;
                var linesToInsert = new List<SaleLineInsertRow>(request.Lines.Count);
                foreach (var line in request.Lines)
                {
                    var product = productsById[line.ProductId];
                    var lineAmount = decimal.Round(product.Price * line.Quantity, 2, MidpointRounding.AwayFromZero);
                    total += lineAmount;

                    linesToInsert.Add(new SaleLineInsertRow
                    {
                        ProductId = line.ProductId,
                        ProductName = product.Name,
                        Quantity = line.Quantity,
                        UnitPrice = product.Price,
                        Amount = lineAmount
                    });
                }

                total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
                var saleCode = await ReserveNextSaleCodeAsync(connection, transaction, currentYear, cancellationToken);

                const string insertSaleQuery = @"
                    INSERT INTO ventas (
                        codigo,
                        clienteId,
                        usuarioId,
                        usuarioUsername,
                        canal,
                        metodoPago,
                        total,
                        estado
                    )
                    VALUES (
                        @Code,
                        @CustomerId,
                        @UserId,
                        @Username,
                        @Channel,
                        @PaymentMethod,
                        @Total,
                        @Status
                    );
                    SELECT LAST_INSERT_ID();";

                var insertSaleCommand = new CommandDefinition(
                    insertSaleQuery,
                    new
                    {
                        Code = saleCode,
                        CustomerId = customerId,
                        UserId = actor.UserId,
                        Username = actor.Username,
                        request.Channel,
                        PaymentMethod = request.PaymentMethod,
                        Total = total,
                        Status = RegisteredStatus
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var saleId = await connection.ExecuteScalarAsync<long>(insertSaleCommand);

                const string insertLineQuery = @"
                    INSERT INTO detalleVenta (
                        ventaId,
                        productId,
                        nombreProductoSnapshot,
                        cantidad,
                        precioUnitario,
                        importe
                    )
                    VALUES (
                        @SaleId,
                        @ProductId,
                        @ProductName,
                        @Quantity,
                        @UnitPrice,
                        @Amount
                    );";

                var lineParameters = new List<object>(linesToInsert.Count);
                foreach (var line in linesToInsert)
                {
                    lineParameters.Add(new
                    {
                        SaleId = saleId,
                        line.ProductId,
                        line.ProductName,
                        line.Quantity,
                        line.UnitPrice,
                        line.Amount
                    });
                }

                var insertLineCommand = new CommandDefinition(
                    insertLineQuery,
                    lineParameters,
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(insertLineCommand);

                const string updateStockQuery = @"
                    UPDATE products
                    SET stock = stock - @Quantity
                    WHERE id = @ProductId
                    AND estado = @ActiveState
                    AND stock >= @Quantity;";

                foreach (var line in linesToInsert)
                {
                    var updateStockCommand = new CommandDefinition(
                        updateStockQuery,
                        new
                        {
                            line.ProductId,
                            line.Quantity,
                            ActiveState
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken);

                    var affectedRows = await connection.ExecuteAsync(updateStockCommand);
                    if (affectedRows == 0)
                    {
                        throw new BusinessValidationException("Lines", "Uno de los productos ya no tiene stock suficiente para completar la venta.");
                    }
                }

                transaction.Commit();
                return saleId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new BusinessValidationException("NewCustomer.DocumentNumber", "Ya existe un cliente registrado con ese CI/NIT.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Los datos de la venta no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("registrar la venta", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("registrar la venta", exception);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> CancelAsync(long saleId, string reason, AuditActor actor, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actor);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
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

                var saleRow = await connection.QueryFirstOrDefaultAsync<SaleStatusRow>(saleCommand);
                if (saleRow == null)
                {
                    transaction.Rollback();
                    return false;
                }

                if (!string.Equals(saleRow.Status, RegisteredStatus, StringComparison.Ordinal))
                {
                    transaction.Rollback();
                    return false;
                }

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

                var saleLines = (await connection.QueryAsync<SaleQuantityRow>(detailCommand)).ToList();

                const string restoreStockQuery = @"
                    UPDATE products
                    SET stock = stock + @Quantity
                    WHERE id = @ProductId;";

                foreach (var saleLine in saleLines)
                {
                    var restoreStockCommand = new CommandDefinition(
                        restoreStockQuery,
                        new
                        {
                            saleLine.ProductId,
                            saleLine.Quantity
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken);

                    await connection.ExecuteAsync(restoreStockCommand);
                }

                const string updateSaleQuery = @"
                    UPDATE ventas
                    SET estado = @CancelledStatus,
                        motivoAnulacion = @Reason,
                        usuarioAnulacionId = @UserId,
                        usuarioAnulacionUsername = @Username,
                        fechaAnulacion = NOW()
                    WHERE id = @SaleId
                    AND estado = @RegisteredStatus;";

                var updateSaleCommand = new CommandDefinition(
                    updateSaleQuery,
                    new
                    {
                        CancelledStatus,
                        Reason = reason,
                        UserId = actor.UserId,
                        Username = actor.Username,
                        SaleId = saleId,
                        RegisteredStatus
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(updateSaleCommand);
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("anular la venta", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("anular la venta", exception);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
