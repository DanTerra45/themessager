using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public string RequestId { get; set; } = string.Empty;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null && !string.IsNullOrEmpty(currentActivity.Id))
        {
            RequestId = currentActivity.Id;
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

