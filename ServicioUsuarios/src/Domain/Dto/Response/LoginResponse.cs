namespace Domain.Dto.Response
{
    public record LoginResponse(
        string accessToken,
        bool needChangePassword
    );
}