using Application.Options;
using Domain.Database;
using Domain.Database.Fields;

namespace Infrastructure.Database
{
    public sealed class CustomerSchema: ITableSchema<CustomerFields>
    {
        private static readonly Dictionary<CustomerFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                CustomerFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                CustomerFields.Ci,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "ci AS Ci" },
                    { SqlAction.Insert, "ci" },
                    { SqlAction.Update, "ci" },
                    { SqlAction.Delete, "ci" }
                }
            },
            {
                CustomerFields.Complement,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "complement AS Complement" },
                    { SqlAction.Insert, "complement" },
                    { SqlAction.Update, "complement" },
                    { SqlAction.Delete, "complement" }
                }
            },
            {
                CustomerFields.RazonSocial,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "razon_social AS RazonSocial" },
                    { SqlAction.Insert, "razon_social" },
                    { SqlAction.Update, "razon_social" },
                    { SqlAction.Delete, "razon_social" }
                }
            },
            {
                CustomerFields.CreatedBy,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_by AS CreatedBy" },
                    { SqlAction.Insert, "created_by" },
                    { SqlAction.Update, "created_by" },
                    { SqlAction.Delete, "created_by" }
                }
            },
            {
                CustomerFields.CreatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_at AS CreatedAt" },
                    { SqlAction.Insert, "created_at" },
                    { SqlAction.Update, "created_at" },
                    { SqlAction.Delete, "created_at" }
                }
            }
        };
        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(CustomerFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}
