using Domain.Database;

namespace Application.Options
{
    public enum SqlAction
    {
        Select,
        Insert,
        Update,
        Delete
    }
    public enum UserFields
    {
        Id,
        Username,
        Email,
        Password,
        Role,
        CreatorId,
        LastLogin,
        NeedPasswordChange,
        State,
        CreatedAt,
        UpdatedAt
    }
    public sealed class UserSchema: ITableSchema<UserFields>
    {
        private static readonly Dictionary<UserFields, Dictionary<SqlAction, string>> Fields = new()
        {
            {
                UserFields.Id,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "id AS Id" },
                    { SqlAction.Insert, "id" },
                    { SqlAction.Update, "id" },
                    { SqlAction.Delete, "id" }
                }
            },
            {
                UserFields.Username,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "username AS Username" },
                    { SqlAction.Insert, "username" },
                    { SqlAction.Update, "username" },
                    { SqlAction.Delete, "username" }
                }
            },
            {
                UserFields.Email,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "email AS Email" },
                    { SqlAction.Insert, "email" },
                    { SqlAction.Update, "email" },
                    { SqlAction.Delete, "email" }
                }
            },
            {
                UserFields.Password,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "password AS Password" },
                    { SqlAction.Insert, "password" },
                    { SqlAction.Update, "password" },
                    { SqlAction.Delete, "password" }
                }
            },
            {
                UserFields.Role,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "role AS Role" },
                    { SqlAction.Insert, "role" },
                    { SqlAction.Update, "role" },
                    { SqlAction.Delete, "role" }
                }
            },
            {
                UserFields.CreatorId,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "creator_id AS CreatorId" },
                    { SqlAction.Insert, "creator_id" },
                    { SqlAction.Update, "creator_id" },
                    { SqlAction.Delete, "creator_id" }
                }
            },
            {
                UserFields.LastLogin,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "last_login AS LastLogin" },
                    { SqlAction.Insert, "last_login" },
                    { SqlAction.Update, "last_login" },
                    { SqlAction.Delete, "last_login" }
                }
            },
            {
                UserFields.NeedPasswordChange,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "need_change_password AS NeedPasswordChange" },
                    { SqlAction.Insert, "need_change_password" },
                    { SqlAction.Update, "need_change_password" },
                    { SqlAction.Delete, "need_change_password" }
                }
            },
            {
                UserFields.State,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "state AS State" },
                    { SqlAction.Insert, "state" },
                    { SqlAction.Update, "state" },
                    { SqlAction.Delete, "state" }
                }
            },
            {
                UserFields.CreatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "created_at AS CreatedAt" },
                    { SqlAction.Insert, "created_at" },
                    { SqlAction.Update, "created_at" },
                    { SqlAction.Delete, "created_at" }
                }
            },
            {
                UserFields.UpdatedAt,
                new Dictionary<SqlAction, string>
                {
                    { SqlAction.Select, "updated_at AS UpdatedAt" },
                    { SqlAction.Insert, "updated_at" },
                    { SqlAction.Update, "updated_at" },
                    { SqlAction.Delete, "updated_at" }
                }
            }
        };
        public string GetAll() => string.Join(", ", Fields.Values.Select(actions => actions[SqlAction.Select]));
        public string Get(UserFields field, SqlAction action)  => Fields.TryGetValue(field, out var actions) && actions.TryGetValue(action, out var value) ? value : throw new Exception($"Field {field} with action {action} not found in schema.");
    }
}