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
    public static class UserFieldsExtensions
    {
        public static string ToColumnName(this UserFields field) => field switch
        {
            UserFields.Id => "id",
            UserFields.Username => "username",
            UserFields.Email => "email",
            UserFields.Password => "password",
            UserFields.Role => "user_role",
            UserFields.CreatorId => "creator_id",
            UserFields.LastLogin => "last_login",
            UserFields.NeedPasswordChange => "need_change_password",
            UserFields.State => "state",
            UserFields.CreatedAt => "created_at",
            UserFields.UpdatedAt => "updated_at",
            _ => throw new ArgumentOutOfRangeException(nameof(field), $"No column mapping defined for {field}")
        };
    }
}