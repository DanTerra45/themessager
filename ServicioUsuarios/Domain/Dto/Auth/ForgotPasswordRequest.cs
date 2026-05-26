namespace Domain.Dto.Auth
{
    public record ForgotPasswordRequest(
        string EmailOrUsername
    );
}