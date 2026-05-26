using Application.Options;
using Domain.Database;
using Domain.Database.Fields;

namespace Infrastructure.Database
{
    public class PasswordResetTokenSchema : ITableSchema<PasswordResetTokenFields>
    {
        private static readonly Dictionary<PasswordResetTokenFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                PasswordResetTokenFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                PasswordResetTokenFields.UserId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "user_id AS UserId" },
                    { SqlAction.Insert, "user_id " },
                    { SqlAction.Update, "user_id " },
                    { SqlAction.Delete, "user_id " }
                }
            },
            {
                PasswordResetTokenFields.Token,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "token_hash AS Token" },
                    { SqlAction.Insert, "token_hash " },
                    { SqlAction.Update, "token_hash " },
                    { SqlAction.Delete, "token_hash " }
                }
            },
            {
                PasswordResetTokenFields.Expiration,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "expires_at AS Expiration" },
                    { SqlAction.Insert, "expires_at " },
                    { SqlAction.Update, "expires_at " },
                    { SqlAction.Delete, "expires_at " }
                }
            },
            {
                PasswordResetTokenFields.UsedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "used_at AS UsedAt" },
                    { SqlAction.Insert, "used_at " },
                    { SqlAction.Update, "used_at " },
                    { SqlAction.Delete, "used_at " }
                }
            },
            {
                PasswordResetTokenFields.CreatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_at AS CreatedAt" },
                    { SqlAction.Insert, "created_at " },
                    { SqlAction.Update, "created_at " },
                    { SqlAction.Delete, "created_at " }
                }
            }
        };

        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(PasswordResetTokenFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}