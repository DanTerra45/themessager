using Mercadito.src.suppliers.application.ports.output;
using System.Data;
using Dapper;
using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.domain.shared.repository;
using Mercadito.src.domain.suppliers.entities;
using Mercadito.src.suppliers.application.models;
using MySqlConnector;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.src.infrastructure.suppliers.persistence
{
    public class SupplierRepository(IDbConnectionFactory dbConnection) 
    : ISupplierRepository, ICrudRepository<CreateSupplierDto, UpdateSupplierDto, Supplier, long>
    {
        private const int SupplierCodeSequenceId = 1;
        private const int MaximumSupplierCodeNumber = 999;

        private static DataStoreUnavailableException CreateDataStoreUnavailableException(string operation, Exception exception)
        {
            return new DataStoreUnavailableException($"No se pudo {operation} porque la base de datos no está disponible.", exception);
        }

        private static string FormatSupplierCode(int codeNumber)
        {
            return $"PRV{codeNumber:000}";
        }

        private static async Task<int> GetFallbackNextSupplierCodeNumberAsync(
            IDbConnection connection,
            IDbTransaction? transaction,
            CancellationToken cancellationToken)
        {
            const string query = @"SELECT COALESCE(MAX(CAST(SUBSTRING(codigo, 4, 3) AS UNSIGNED)), 0) + 1
                FROM proveedores";

            var command = new CommandDefinition(query, transaction: transaction, cancellationToken: cancellationToken);
            var nextCodeNumber = await connection.ExecuteScalarAsync<int>(command);
            if (nextCodeNumber < 1)
            {
                return 1;
            }

            return nextCodeNumber;
        }

        private static async Task<string> ReserveNextSupplierCodeAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string ensureSequenceRowQuery = @"INSERT INTO supplier_code_sequence (`id`, `nextValue`)
                VALUES (@SequenceId, 1)
                ON DUPLICATE KEY UPDATE `nextValue` = `nextValue`";

            var ensureSequenceRowCommand = new CommandDefinition(
                ensureSequenceRowQuery,
                new { SequenceId = SupplierCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(ensureSequenceRowCommand);

            const string lockAndReadQuery = @"SELECT `nextValue`
                FROM supplier_code_sequence
                WHERE `id` = @SequenceId
                FOR UPDATE";

            var lockAndReadCommand = new CommandDefinition(
                lockAndReadQuery,
                new { SequenceId = SupplierCodeSequenceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var reservedCodeNumber = await connection.ExecuteScalarAsync<int?>(lockAndReadCommand);
            var nextCodeNumber = reservedCodeNumber.GetValueOrDefault();
            if (nextCodeNumber <= 0)
            {
                nextCodeNumber = await GetFallbackNextSupplierCodeNumberAsync(connection, transaction, cancellationToken);
            }

            if (nextCodeNumber > MaximumSupplierCodeNumber)
            {
                throw new BusinessValidationException("Codigo", "No hay más códigos de proveedor disponibles.");
            }

            const string updateSequenceQuery = @"UPDATE supplier_code_sequence
                SET `nextValue` = @FollowingCodeNumber
                WHERE `id` = @SequenceId";

            var updateSequenceCommand = new CommandDefinition(
                updateSequenceQuery,
                new
                {
                    SequenceId = SupplierCodeSequenceId,
                    FollowingCodeNumber = nextCodeNumber + 1
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(updateSequenceCommand);
            return FormatSupplierCode(nextCodeNumber);
        }

        public async Task<string> GetNextSupplierCodeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
                int nextCodeNumber;

                try
                {
                    const string query = @"SELECT `nextValue`
                    FROM supplier_code_sequence
                    WHERE `id` = @SequenceId";

                    var command = new CommandDefinition(
                        query,
                        new { SequenceId = SupplierCodeSequenceId },
                        cancellationToken: cancellationToken);

                    var codeNumberFromSequence = await connection.ExecuteScalarAsync<int?>(command);
                    if (codeNumberFromSequence.HasValue && codeNumberFromSequence.Value > 0)
                    {
                        nextCodeNumber = codeNumberFromSequence.Value;
                    }
                    else
                    {
                        nextCodeNumber = await GetFallbackNextSupplierCodeNumberAsync(connection, transaction: null, cancellationToken);
                    }
                }
                catch (MySqlException exception) when (exception.Number == 1146)
                {
                    nextCodeNumber = await GetFallbackNextSupplierCodeNumberAsync(connection, transaction: null, cancellationToken);
                }

                if (nextCodeNumber > MaximumSupplierCodeNumber)
                {
                    throw new BusinessValidationException("Codigo", "No hay más códigos de proveedor disponibles.");
                }

                return FormatSupplierCode(nextCodeNumber);
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el siguiente código de proveedor", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el siguiente código de proveedor", exception);
            }
        }

        public async Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"
            SELECT
                p.id AS Id,
                p.codigo AS Codigo,
                p.razonSocial AS RazonSocial,
                p.direccion AS Direccion,
                p.contacto AS Contacto,
                p.telefono AS Telefono,
                p.rubro AS Rubro
            FROM proveedores p
            WHERE p.estado = 'A'
            ORDER BY p.razonSocial ASC, p.id ASC";
                var command = new CommandDefinition(query, cancellationToken: cancellationToken);
                var suppliers = await connection.QueryAsync<Supplier>(command);
                return [.. suppliers];
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar los proveedores", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar los proveedores", exception);
            }
        }
        public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
                const string query = @"
            SELECT
                p.id AS Id,
                p.codigo AS Codigo,
                p.razonSocial AS RazonSocial,
                p.direccion AS Direccion,
                p.contacto AS Contacto,
                p.telefono AS Telefono,
                p.rubro AS Rubro
            FROM proveedores p 
                WHERE p.id = @Id AND p.estado = 'A'";
                var command = new CommandDefinition(query, parameters: new { Id = id }, cancellationToken: cancellationToken);
                var supplierForEditRow = await connection.QueryFirstOrDefaultAsync<Supplier>(command);
                if (supplierForEditRow == null)
                {
                    return null;
                }
                return supplierForEditRow;
            }
            catch (MySqlException exception)
            {
                throw CreateDataStoreUnavailableException("consultar el proveedor", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                throw CreateDataStoreUnavailableException("consultar el proveedor", exception);
            }
        }
        public async Task<long> CreateAsync (CreateSupplierDto entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                var reservedCode = await ReserveNextSupplierCodeAsync(connection, transaction, cancellationToken);
                const string query = @"
                INSERT INTO proveedores (codigo, razonSocial, direccion, contacto, telefono, rubro)
                VALUES (@Codigo, @Nombre, @Direccion, @Contacto, @Telefono, @Rubro);
                SELECT LAST_INSERT_ID();";
                var command = new CommandDefinition(
                    query,
                    parameters: new
                    {
                        Codigo = reservedCode,
                        entity.Nombre,
                        entity.Direccion,
                        entity.Contacto,
                        entity.Telefono,
                        entity.Rubro
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);
                var id = await connection.ExecuteScalarAsync<long>(command);
                transaction.Commit();
                return id;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Codigo", "Ya existe un proveedor con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Los datos del proveedor no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el proveedor", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("crear el proveedor", exception);
            }
        }
        public async Task<int> UpdateAsync(UpdateSupplierDto entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                const string query = @"
                UPDATE proveedores
                SET
                    codigo = @Codigo,
                    razonSocial = @Nombre,
                    direccion = @Direccion,
                    contacto = @Contacto,
                    telefono = @Telefono,
                    rubro = @Rubro
                WHERE id = @Id;";
                var command = new CommandDefinition(query, parameters: entity,transaction: transaction ,cancellationToken: cancellationToken);
                var rowsAffected = await connection.ExecuteAsync(command);
                transaction.Commit();
                return rowsAffected;
            }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Codigo", "Ya existe un proveedor con ese código.");
            }
            catch (MySqlException exception) when (exception.Number == 3819)
            {
                transaction.Rollback();
                throw new BusinessValidationException("Los datos del proveedor no cumplen el formato requerido.");
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("actualizar el proveedor", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("actualizar el proveedor", exception);
            }
        }
        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                var query = "UPDATE proveedores SET estado = 'I' WHERE id = @Id AND estado = 'A'";
                var command = new CommandDefinition(query, parameters: new { Id = id },transaction: transaction ,cancellationToken: cancellationToken);
                var rowsAffected = await connection.ExecuteAsync(command);
                transaction.Commit();
                return rowsAffected;
            }
            catch (MySqlException exception)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("desactivar el proveedor", exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is MySqlException)
            {
                transaction.Rollback();
                throw CreateDataStoreUnavailableException("desactivar el proveedor", exception);
            }
        }
    }
}
