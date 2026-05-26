using Application.Emails.Templates;
using Application.Utils;
using Domain.Dto.Email;

namespace Application.Service;

public sealed class EmailService
{
    private readonly MailKitEmailSender _sender;

    public EmailService(MailKitEmailSender sender)
    {
        _sender = sender;
    }

    public Task SendOnboardingAsync(
        string toAddress,
        string username,
        string role,
        string temporaryPassword,
        string actionUrl)
    {
        var html = UserAccessEmailTemplate.BuildOnboardingHtml(
            username,
            role,
            temporaryPassword,
            actionUrl);
        var plainText = string.Join(Environment.NewLine, new[]
        {
            $"Hola {username},",
            string.Empty,
            "Tu cuenta fue creada correctamente.",
            $"Rol asignado: {role}.",
            $"Tu contraseña temporal es: {temporaryPassword}",
            "Debes cambiarla al ingresar.",
            string.Empty,
            $"Accede aquí: {actionUrl}"
        });

        var mail = new MailMessage(
            ToAddress: toAddress,
            ToName: username,
            Subject: "Activa tu acceso",
            PlainTextBody: plainText,
            HtmlBody: html
        );

        return _sender.SendAsync(mail);
    }

    public Task SendPasswordResetAsync(
        string toAddress,
        string username,
        string resetUrl)
    {
        var html = UserAccessEmailTemplate.BuildPasswordResetHtml(username, resetUrl);
        var plainText = string.Join(Environment.NewLine, new[]
        {
            $"Hola {username},",
            string.Empty,
            "Recibimos una solicitud para restablecer tu contraseña.",
            "El enlace de restablecimiento tiene una duración de 30 minutos.",
            "Al finalizar, deberás iniciar sesión con la nueva contraseña que definas.",
            string.Empty,
            $"Abre este enlace: {resetUrl}",
            string.Empty,
            "Si no solicitaste este cambio, puedes ignorar este mensaje."
        });

        var mail = new MailMessage(
            ToAddress: toAddress,
            ToName: username,
            Subject: "Restablece tu contraseña",
            PlainTextBody: plainText,
            HtmlBody: html
        );

        return _sender.SendAsync(mail);
    }

    public Task SendAdministrativeResetAsync(
        string toAddress,
        string username,
        string temporaryPassword,
        string actionUrl)
    {
        var html = UserAccessEmailTemplate.BuildAdministrativeResetHtml(username, temporaryPassword, actionUrl);
        var plainText = string.Join(Environment.NewLine, new[]
        {
            $"Hola {username},",
            string.Empty,
            "Un administrador reinició tu acceso al sistema.",
            "Tu contraseña anterior dejó de ser válida.",
            string.Empty,
            $"Tu contraseña temporal es: {temporaryPassword}",
            string.Empty,
            $"Usa este enlace: {actionUrl}",
            string.Empty,
            "Si no reconoces este cambio, comunícate con un administrador."
        });

        var mail = new MailMessage(
            ToAddress: toAddress,
            ToName: username,
            Subject: "Un administrador reinició tu acceso",
            PlainTextBody: plainText,
            HtmlBody: html
        );

        return _sender.SendAsync(mail);
    }
}