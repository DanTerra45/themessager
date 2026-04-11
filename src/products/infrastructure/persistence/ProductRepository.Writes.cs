using Dapper;
using Mercadito.src.products.application.models;
using MySqlConnector;
using Mercadito.src.shared.domain.exceptions;

namespace Mercadito.src.products.infrastructure.persistence
{
    public partial class ProductRepository
    {
        public async Task<long> CreateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(productWithCategories);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var product = productWithCategories.Product;
                var normalizedCategoryIds = NormalizeCategoryIds(productWithCategories.CategoryIds);
                await EnsureAllCategoriesAreActiveAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                const string insertProductQuery = "INSERT INTO products (nombre, descripcion, stock, lote, fechaCaducidad, precio, estado) VALUES (@Name, @Description, @Stock, @Batch, @ExpirationDate, @Price, @ActiveState); SELECT LAST_INSERT_ID();";
                var insertProductCommand = new CommandDefinition(
                    insertProductQuery,
                    parameters: new
                    {
                        product.Name,
                        product.Description,
                        product.Stock,
                        product.Batch,
                        ExpirationDate = ToDateTime(product.ExpirationDate),
                        product.Price,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var createdProductId = await connection.ExecuteScalarAsync<long>(insertProductCommand);

                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        createdProductId,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return createdProductId;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new BusinessValidationException(new Dictionary<string, List<string>>
                {
                    ["Name"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."],
                    ["Batch"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."],
                    ["ExpirationDate"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."]
                });
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Los datos del producto no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("guardar el producto", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("guardar el producto", exception);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> UpdateAsync(ProductWithCategoriesWriteModel productWithCategories, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(productWithCategories);

            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var product = productWithCategories.Product;
                var normalizedCategoryIds = NormalizeCategoryIds(productWithCategories.CategoryIds);
                var currentCategoryIdsCommand = BuildRelatedCategoryIdsByProductCommand(
                    product.Id,
                    transaction,
                    cancellationToken);
                var currentCategoryIds = (await connection.QueryAsync<long>(currentCategoryIdsCommand)).ToList();
                var touchedCategoryIds = MergeCategoryIds(currentCategoryIds, normalizedCategoryIds);

                const string updateProductQuery = @"UPDATE products
                    SET nombre = @Name,
                        descripcion = @Description,
                        stock = @Stock,
                        lote = @Batch,
                        fechaCaducidad = @ExpirationDate,
                        precio = @Price
                    WHERE id = @Id AND estado = @ActiveState";

                var updateProductCommand = new CommandDefinition(
                    updateProductQuery,
                    parameters: new
                    {
                        product.Id,
                        product.Name,
                        product.Description,
                        product.Stock,
                        product.Batch,
                        ExpirationDate = ToDateTime(product.ExpirationDate),
                        product.Price,
                        ActiveState
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(updateProductCommand);

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                await EnsureAllCategoriesAreActiveAsync(
                    connection,
                    normalizedCategoryIds,
                    transaction,
                    cancellationToken);

                const string deleteRelationsQuery = @"DELETE FROM categoriaDeProducto
                    WHERE productId = @ProductId";

                var deleteRelationsCommand = new CommandDefinition(
                    deleteRelationsQuery,
                    parameters: new { ProductId = product.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(deleteRelationsCommand);

                if (normalizedCategoryIds.Count > 0)
                {
                    var insertCategoriesCommand = BuildInsertProductCategoriesCommand(
                        product.Id,
                        normalizedCategoryIds,
                        transaction,
                        cancellationToken);

                    await connection.ExecuteAsync(insertCategoriesCommand);
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    touchedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return affectedRows;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new BusinessValidationException(new Dictionary<string, List<string>>
                {
                    ["Name"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."],
                    ["Batch"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."],
                    ["ExpirationDate"] = ["Ya existe un producto activo con el mismo nombre, lote y fecha de caducidad."]
                });
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Los datos del producto no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("actualizar el producto", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("actualizar el producto", exception);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await _dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                var relatedCategoryIdsCommand = BuildRelatedCategoryIdsByProductCommand(
                    id,
                    transaction,
                    cancellationToken);
                var relatedCategoryIds = (await connection.QueryAsync<long>(relatedCategoryIdsCommand)).AsList();

                const string query = @"UPDATE products
                    SET estado = @InactiveState
                    WHERE id = @Id AND estado = @ActiveState";

                var command = new CommandDefinition(
                    query,
                    parameters: new { Id = id, ActiveState, InactiveState },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                var affectedRows = await connection.ExecuteAsync(command);
                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                await RecalculateCategoryProductCountsAsync(
                    connection,
                    relatedCategoryIds,
                    transaction,
                    cancellationToken);

                transaction.Commit();
                return affectedRows;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("eliminar el producto", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("eliminar el producto", exception);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

    }
}
