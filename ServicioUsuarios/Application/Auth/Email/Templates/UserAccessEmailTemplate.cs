using System.Net;
using System.Text;

namespace Application.Emails.Templates
{
    public static class UserAccessEmailTemplate
    {
        public static string BuildOnboardingHtml(
            string username,
            string role,
            string temporaryPassword,
            string actionUrl)
        {
            return BuildDocument(
                "Activa tu acceso",
                username,
                [
                    "Se creó tu acceso al sistema.",
                    $"Rol asignado: <strong>{Encode(role)}</strong>.",
                    $"Tu contraseña temporal es: <strong>{Encode(temporaryPassword)}</strong>.",
                    "Al iniciar sesión por primera vez deberás cambiarla."
                ],
                "Iniciar sesión",
                "El cambio de contraseña se realizará al ingresar.",
                "Si no esperabas este mensaje, contacta al administrador del sistema.",
                actionUrl);
        }

        public static string BuildAdministrativeResetHtml(
            string username,
            string temporaryPassword,
            string actionUrl)
        {
            return BuildDocument(
                "Un administrador reinició tu acceso",
                username,
                [
                    "Un administrador reinició tu acceso al sistema.",
                    "Tu contraseña anterior dejó de ser válida.",
                    $"Tu nueva contraseña temporal es: <strong>{Encode(temporaryPassword)}</strong>.",
                    "Al iniciar sesión deberás cambiarla por una nueva."
                ],
                "Iniciar sesión",
                "El cambio de contraseña se realizará al ingresar.",
                "Si no reconoces este cambio, comunícate con un administrador.",
                actionUrl);
        }

        public static string BuildPasswordResetHtml(
            string username,
            string actionUrl)
        {
            return BuildDocument(
                "Restablece tu contraseña",
                username,
                [
                    "Recibimos una solicitud para restablecer tu contraseña.",
                    "El enlace de restablecimiento tiene una duración de 30 minutos.",
                    "Al finalizar, deberás iniciar sesión con la nueva contraseña que definas."
                ],
                "Restablecer contraseña",
                "Si el botón no funciona, copia y pega el enlace en tu navegador",
                "Si no solicitaste este cambio, puedes ignorar este mensaje.",
                actionUrl);
        }

        private static string BuildDocument(
            string title,
            string username,
            IReadOnlyList<string> paragraphs,
            string actionLabel,
            string notice,
            string footerText,
            string actionUrl)
        {
            const string sansStack = "'Segoe UI', Arial, Helvetica, sans-serif";
            var safeTitle = Encode(title);
            var safeUsername = Encode(username);
            var safeActionLabel = Encode(actionLabel);
            var safeNotice = Encode(notice);
            var safeFooterText = Encode(footerText);
            var safeActionUrl = Encode(actionUrl);

            var paragraphBuilder = new StringBuilder();
            foreach (var paragraph in paragraphs)
            {
                paragraphBuilder.Append($"<p style=\"margin:0 0 16px 0;font-family:{sansStack};font-size:16px;line-height:1.6;color:#0d1f15;\">");
                paragraphBuilder.Append(paragraph);
                paragraphBuilder.Append("</p>");
            }

            return
                "<!doctype html>" +
                "<html lang=\"es\">" +
                "<head>" +
                "<meta charset=\"utf-8\">" +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                $"<title>{safeTitle}</title>" +
                "</head>" +
                "<body style=\"margin:0;padding:0;background-color:#f5f4f0;\">" +
                "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background-color:#f5f4f0;border-collapse:collapse;\">" +
                "<tr>" +
                "<td align=\"center\" style=\"padding:32px 16px;\">" +
                "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:640px;border-collapse:collapse;\">" +
                "<tr>" +
                $"<td style=\"padding:0 0 16px 0;font-family:{sansStack};font-size:28px;font-weight:800;letter-spacing:-0.03em;color:#0d1f15;text-transform:uppercase;\">Mercadito</td>" +
                "</tr>" +
                "<tr>" +
                "<td style=\"background-color:#ffffff;border:2px solid #0d1f15;box-shadow:6px 6px 0 #0d1f15;\">" +
                "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"border-collapse:collapse;\">" +
                "<tr>" +
                "<td style=\"padding:18px 24px;background-color:#e9e8e2;border-bottom:2px solid #0d1f15;\">" +
                $"<div style=\"margin:0 0 6px 0;font-family:{sansStack};font-size:12px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;color:#4a5c51;\">Notificación de acceso</div>" +
                $"<div style=\"margin:0;font-family:{sansStack};font-size:28px;font-weight:800;line-height:1.1;color:#0d1f15;\">{safeTitle}</div>" +
                "</td>" +
                "</tr>" +
                "<tr>" +
                "<td style=\"padding:28px 24px 12px 24px;\">" +
                $"<p style=\"margin:0 0 16px 0;font-family:{sansStack};font-size:16px;line-height:1.6;color:#0d1f15;\">Hola <strong>{safeUsername}</strong>,</p>" +
                paragraphBuilder +
                "<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:24px 0 20px 0;border-collapse:collapse;\">" +
                "<tr>" +
                "<td style=\"background-color:#00e5ff;border:2px solid #0d1f15;box-shadow:4px 4px 0 #0d1f15;\">" +
                $"<a href=\"{safeActionUrl}\" style=\"display:inline-block;padding:14px 22px;font-family:{sansStack};font-size:15px;font-weight:700;line-height:1.2;color:#0d1f15;text-decoration:none;text-transform:uppercase;\">{safeActionLabel}</a>" +
                "</td>" +
                "</tr>" +
                "</table>" +
                $"<p style=\"margin:0 0 14px 0;font-family:{sansStack};font-size:14px;line-height:1.6;color:#4a5c51;\">{safeNotice}</p>" +
                $"<a href=\"{safeActionUrl}\" style=\"margin:0 0 14px 0;font-family:{sansStack};font-size:14px;line-height:1.6;color:#4a5c51; text-decoration: underline;\">{safeActionUrl}</a>" +
                $"<p style=\"margin:0 0 22px 0;font-family:{sansStack};font-size:14px;line-height:1.6;color:#4a5c51;\">{safeFooterText}</p>" +
                "</td>" +
                "</tr>" +
                "<tr>" +
                "<td style=\"padding:18px 24px;border-top:2px solid #0d1f15;background-color:#ffffff;\">" +
                $"<p style=\"margin:0;font-family:{sansStack};font-size:12px;line-height:1.6;color:#4a5c51;\">Mercadito · Mensaje automático del sistema</p>" +
                "</td>" +
                "</tr>" +
                "</table>" +
                "</td>" +
                "</tr>" +
                "</table>" +
                "</td>" +
                "</tr>" +
                "</table>" +
                "</body>" +
                "</html>";
        }

        private static string Encode(string value)
        {
            return WebUtility.HtmlEncode(value);
        }
    }
}