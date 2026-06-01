using Domain.Database;

namespace Domain.Database.Fields
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
}