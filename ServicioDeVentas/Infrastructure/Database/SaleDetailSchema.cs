using Application.Options;
using Domain.Database;
using Domain.Database.Fields;

namespace Infrastructure.Database
{
    public sealed class SaleDetailSchema: ITableSchema<SaleDetailFields>
    {
        private static readonly Dictionary<SaleDetailFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                SaleDetailFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                SaleDetailFields.SaleId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "sale_id AS SaleId" },
                    { SqlAction.Insert, "sale_id" },
                    { SqlAction.Update, "sale_id" },
                    { SqlAction.Delete, "sale_id" }
                }
            },
            {
                SaleDetailFields.ProductId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "product_id AS ProductId" },
                    { SqlAction.Insert, "product_id" },
                    { SqlAction.Update, "product_id" },
                    { SqlAction.Delete, "product_id" }
                }
            },
            {
                SaleDetailFields.Quantity,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "quantity AS Quantity" },
                    { SqlAction.Insert, "quantity" },
                    { SqlAction.Update, "quantity" },
                    { SqlAction.Delete, "quantity" }
                }
            },
            {
                SaleDetailFields.UnitPrice,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "unit_price AS UnitPrice" },
                    { SqlAction.Insert, "unit_price" },
                    { SqlAction.Update, "unit_price" },
                    { SqlAction.Delete, "unit_price" }
                }
            },
            {
                SaleDetailFields.SubTotal,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "sub_total AS SubTotal" },
                    { SqlAction.Insert, "sub_total" },
                    { SqlAction.Update, "sub_total" },
                    { SqlAction.Delete, "sub_total" }
                }
            }
        };
        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(SaleDetailFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}
