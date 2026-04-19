using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Mercadito.src.domain.shared.exceptions;

namespace Mercadito.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
    public string? RequestId { get; set; }
    public string UserMessage { get; set; } = "Ha ocurrido un error genérico al procesar su solicitud.";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        if (Activity.Current is not null && !string.IsNullOrWhiteSpace(Activity.Current.Id))
        {
            RequestId = Activity.Current.Id;
        }
        else
        {
            RequestId = HttpContext.TraceIdentifier;
        }

        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error is not null)
        {
            logger.LogError(exceptionFeature.Error, "Excepción no manejada en la ruta {Path}", exceptionFeature.Path);

            if (IsDatabaseConnectionError(exceptionFeature.Error))
            {
                UserMessage = "La conexión con la base de datos no está disponible. Por favor, intente más tarde.";
            }
        }
    }

    private static bool IsDatabaseConnectionError(Exception exception)
    {
        if (exception is DataStoreUnavailableException)
        {
            return true;
        }

        if (exception.Message.Contains("No se pudo abrir una conexión con la base de datos", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("Unable to connect to any of the specified MySQL hosts", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (exception.InnerException is not null)
        {
            return IsDatabaseConnectionError(exception.InnerException);
        }

        return false;
    }
}
