using Application.Options;
using Dapper;
using Domain.Common;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Entities;
using Domain.Repository;
using Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repository
{
    public class SaleRepository :  BaseRepository<SaleWithDetails, int, SaleFields, SaleOptions, SaleSchema> , ISaleRepository
    {
        public SaleRepository(
            IDbConnectionFactory db, 
            ILogger<SaleRepository> logger
        ) : base(db, "sales", logger)
        {
        }

        private (string Sql, DynamicParameters Parameters) BuildSalesWithDetailsQuery(SaleOptions? options)
        {
            var queryBuilder = new QueryBuilder<SaleOptions, SaleFields>(_tableName, new SaleSchema());
            var (salesBaseSql, parameters) = queryBuilder
                .Select(options ?? new SaleOptions())
                .Where(options ?? new SaleOptions())
                .Paginate(options ?? new SaleOptions())
                .Build();

            var sql = $"""
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
                    sd.id AS DetailId, 
                    sd.sale_id AS SaleId, 
                    sd.product_id AS ProductId, 
                    sd.quantity AS Quantity, 
                    sd.unit_price AS UnitPrice, 
                    sd.subtotal AS SubTotal
                FROM sl
                LEFT JOIN sale_details sd ON sl.id = sd.sale_id
                """;

            return (sql, parameters);
        }

        private async Task<Dictionary<int, SaleWithDetails>> FetchSalesWithDetailsAsync(
            string sql,
            DynamicParameters parameters
        )
        {
            using var connection = await _db.CreateConnectionAsync();
            var saleDictionary = new Dictionary<int, SaleWithDetails>();

            await connection.QueryAsync<Sale, SaleDetails, SaleWithDetails>(
                sql,
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
                splitOn: "DetailId"
            );

            return saleDictionary;
        }

        public override async Task<Result<IEnumerable<SaleWithDetails>>> GetAllAsync(SaleOptions? options)
        {
            var (finalSql, parameters) = BuildSalesWithDetailsQuery(options);

            this._logger.LogInformation(
                "Executing SQL: {Sql} with parameters: {@Parameters}", 
                finalSql, 
                parameters
            );

            try
            {
                var saleDictionary = await FetchSalesWithDetailsAsync(finalSql, parameters);
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
        public override async Task<Result<SaleWithDetails>> GetOneAsync(SaleOptions? options)
        {
            var (finalSql, parameters) = BuildSalesWithDetailsQuery(options);

            this._logger.LogInformation(
                "Executing SQL: {Sql} with parameters: {@Parameters}",
                finalSql,
                parameters
            );

            try
            {
                var saleDictionary = await FetchSalesWithDetailsAsync(finalSql, parameters);
                var sale = saleDictionary.Values.FirstOrDefault();

                if (sale is null)
                {
                    return Result<SaleWithDetails>.Failure(
                        new AppError(
                            "SaleNotFound",
                            "No se encontró ninguna venta que coincida con los criterios proporcionados.",
                            ErrorType.NotFound
                        )
                    );
                }

                return Result<SaleWithDetails>.Success(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while executing SQL: {Sql} with parameters: {@Parameters}",
                    finalSql,
                    parameters
                );
                return Result<SaleWithDetails>.Failure(
                    new AppError(ex.GetType().Name, ex.Message, ErrorType.Internal)
                );
            }
        }
    }
}