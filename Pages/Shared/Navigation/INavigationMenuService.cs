using System.Security.Claims;

namespace Mercadito.Pages.Shared.Navigation
{
    public interface INavigationMenuService
    {
        NavigationMenuViewModel Build(ClaimsPrincipal user, string currentUrl);
    }
}
