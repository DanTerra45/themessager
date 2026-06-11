using Application.Options;
using Dapper;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repository
{
    public class SaleRepository : BaseRepository<SaleWithDetails, int, SaleFields, SaleOptions, SaleSchema> 
    {
        public SaleRepository(
            IDbConnectionFactory db, 
            ILogger<SaleRepository> logger
        ) : base(db, "sales", logger)
        {
        }

        public override async Task<Result<IEnumerable<SaleWithDetails>>> GetAllAsync(SaleOptions? options)
        {
            // 1. Dejamos que el QueryBuilder arme la consulta completa, filtrada y paginada 
            //    SOLO para la tabla 'sales' (igual a como lo hace tu BaseRepository)
            var queryBuilder = new QueryBuilder<SaleOptions, SaleFields>(_tableName, new SaleSchema());
            var (salesBaseSql, parameters) = queryBuilder
                .Select(options ?? new SaleOptions()) 
                .Where(options ?? new SaleOptions())
                .Paginate(options ?? new SaleOptions())
                .Build();

            // 2. Envolvemos esa consulta limpia en un CTE (WITH) para hacer el LEFT JOIN con los detalles.
            //    De esta forma la paginación funciona perfectamente sobre las ventas reales.
            var finalSql = $"""
                WITH sl AS (
                    {salesBaseSql}
                )
                SELECT 
                    sl.id AS Id, 
                    sl.CustomerId AS CustomerId,
                    sl.OperatorId AS OperatorId, 
                    sl.TotalPrice AS TotalPrice, 
                    sl.CreatedAt AS CreatedAt, 
                    sl.state AS State,
                    
                    sd.id AS Id, 
                    sd.sale_id AS SaleId, 
                    sd.product_id AS ProductId, 
                    sd.quantity AS Quantity, 
                    sd.unit_price AS UnitPrice, 
                    sd.subtotal AS SubTotal
                FROM sl
                LEFT JOIN sale_details sd ON sl.id = sd.sale_id
                """;

            this._logger.LogInformation(
                "Executing SQL: {Sql} with parameters: {@Parameters}", 
                finalSql, 
                parameters
            );

            try
            {
                using var connection = await _db.CreateConnectionAsync();
                var saleDictionary = new Dictionary<int, SaleWithDetails>();

                await connection.QueryAsync<Sale, SaleDetails, SaleWithDetails>(
                    finalSql,
                    (sale, detail) =>
                    {
                        if (!saleDictionary.TryGetValue(sale.Id, out var currentSale))
                        {
                            currentSale = new SaleWithDetails(
                                sale.Id,
                                sale.CustomerId,
                                sale.OperatorId,
                                sale.TotalPrice,
                                sale.CreatedAt,
                                sale.State,
                                new List<SaleDetails>()
                            );
                            saleDictionary.Add(currentSale.Id, currentSale);
                        }

                        if (detail != null)
                        {
                            ((List<SaleDetails>)currentSale.Details).Add(detail);
                        }

                        return currentSale;
                    },
                    param: parameters,
                    splitOn: "Id"
                );
                
                return Result<IEnumerable<SaleWithDetails>>.Success(saleDictionary.Values.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while executing SQL: {Sql} with parameters: {@Parameters}",
                    finalSql,
                    parameters
                );
                return Result<IEnumerable<SaleWithDetails>>.Failure(
                    new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal)
                );
            }
        }
    }
}