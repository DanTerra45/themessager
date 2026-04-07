using System.Data;
using Dapper;
using Mercadito.database.interfaces;
using Mercadito.src.shared.domain.repository;
using Mercadito.src.domain.provedores.model;
using Mercadito.src.domain.provedores.dto;

namespace Mercadito.src.domain.provedores.repository
{
    public class SupplierRepository(IDbConnectionFactory dbConnection) 
    : ICrudRepository<CreateSupplierDto, UpdateSupplierDto, Supplier, long>
    {
        public async Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
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
            FROM proveedores p";
            var command = new CommandDefinition(query, cancellationToken: cancellationToken);
            var suppliers = await connection.QueryAsync<Supplier>(command);
            return suppliers.ToList();
        }
        public async Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
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
                WHERE p.id = @Id";
            var command = new CommandDefinition(query, parameters: new { Id = id }, cancellationToken: cancellationToken);
            var supplierForEditRow = await connection.QueryFirstOrDefaultAsync<Supplier>(command);
            if (supplierForEditRow == null)
            {
                return null;
            }
            return supplierForEditRow;
        }
        public async Task<long> CreateAsync (CreateSupplierDto entity, CancellationToken cancellationToken = default)
        {
            using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                const string query = @"
                INSERT INTO proveedores (codigo, razonSocial, direccion, contacto, telefono, rubro)
                VALUES (@Codigo, @Nombre, @Direccion, @Contacto, @Telefono, @Rubro);
                SELECT LAST_INSERT_ID();";
                var command = new CommandDefinition(query, parameters: entity,transaction: transaction ,cancellationToken: cancellationToken);
                var id = await connection.ExecuteScalarAsync<long>(command);
                transaction.Commit();
                return id;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<int> UpdateAsync(UpdateSupplierDto entity, CancellationToken cancellationToken = default)
        {
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
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            using var connection = await dbConnection.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();
            try
            {
                var query = "DELETE FROM proveedores WHERE id = @Id";
                var command = new CommandDefinition(query, parameters: new { Id = id },transaction: transaction ,cancellationToken: cancellationToken);
                var rowsAffected = await connection.ExecuteAsync(command);
                transaction.Commit();
                return rowsAffected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}