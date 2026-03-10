using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public string Message { get; set; } = "Ocurrio un error al procesar tu solicitud.";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

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

        _logger.LogError(
            "Unhandled request reached Error page. RequestId: {RequestId}, Path: {Path}",
            RequestId,
            HttpContext.Request.Path.Value);
    }
}

