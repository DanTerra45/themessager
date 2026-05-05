using System.Security.Claims;

namespace Mercadito.Frontend.Pages.Shared.Navigation;

public interface INavigationMenuService
{
    NavigationMenuViewModel Build(ClaimsPrincipal user);
}
