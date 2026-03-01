using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercadito.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDataBaseConnection _dbConnection;

    public IndexModel(ILogger<IndexModel> logger, IDataBaseConnection dbConnection)
    {
        _logger = logger;
        _dbConnection = dbConnection;
    }

    public void OnGet()
    {
        
    }
}
