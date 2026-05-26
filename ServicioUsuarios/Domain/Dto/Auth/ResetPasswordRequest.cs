namespace Domain.Dto.Auth;

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword
);