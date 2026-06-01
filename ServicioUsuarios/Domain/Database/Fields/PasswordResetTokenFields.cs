namespace Domain.Database.Fields
{
    public enum PasswordResetTokenFields
    {
        Id,
        UserId,
        Token,
        UsedAt,
        Expiration,
        CreatedAt
    }
}