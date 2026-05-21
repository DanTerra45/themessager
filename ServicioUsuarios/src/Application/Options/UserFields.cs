using Domain.Database;

namespace Application.Options
{
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
        private static Dictionary<UserFields, string> Fields = new()
        {
            { UserFields.Id, "id AS Id" },
            { UserFields.Username, "username AS Username" },
            { UserFields.Email, "email AS Email" },
            { UserFields.Password, "password AS Password" },
            { UserFields.Role, "role AS Role" },
            { UserFields.CreatorId, "creator_id AS CreatorId" },
            { UserFields.LastLogin, "last_login AS LastLogin" },
            { UserFields.NeedPasswordChange, "need_change_password AS NeedPasswordChange" },
            { UserFields.State, "state AS State" },
            { UserFields.CreatedAt, "created_at AS CreatedAt" },
            { UserFields.UpdatedAt, "updated_at AS UpdatedAt" }
        };
        public string GetAll() => string.Join(", ", Fields.Values);
        public string Get(UserFields field) => Fields.TryGetValue(field, out var columnName) ? columnName : throw new ArgumentException($"Invalid field: {field}");
    }
}