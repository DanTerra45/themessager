using Application.Options;
using Domain.Database;
using Domain.Database.Fields;

namespace Infrastructure.Database
{
    public sealed class SaleSchema: ITableSchema<SaleFields>
    {
        private static readonly Dictionary<SaleFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                SaleFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                SaleFields.CustomerId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "customer_id AS CustomerId" },
                    { SqlAction.Insert, "customer_id" },
                    { SqlAction.Update, "customer_id" },
                    { SqlAction.Delete, "customer_id" }
                }
            },
            {
                SaleFields.OperatorId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "operator_id AS OperatorId" },
                    { SqlAction.Insert, "operator_id" },
                    { SqlAction.Update, "operator_id" },
                    { SqlAction.Delete, "operator_id" }
                }
            },
            {
                SaleFields.TotalPrice,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "total_price AS TotalPrice" },
                    { SqlAction.Insert, "total_price" },
                    { SqlAction.Update, "total_price" },
                    { SqlAction.Delete, "total_price" }
                }
            },
            {
                SaleFields.CreatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_at AS CreatedAt" },
                    { SqlAction.Insert, "created_at" },
                    { SqlAction.Update, "created_at" },
                    { SqlAction.Delete, "created_at" }
                }
            },
            {
                SaleFields.State,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "state AS State" },
                    { SqlAction.Insert, "state" },
                    { SqlAction.Update, "state" },
                    { SqlAction.Delete, "state" }
                }
            }
        };
        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(SaleFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}
