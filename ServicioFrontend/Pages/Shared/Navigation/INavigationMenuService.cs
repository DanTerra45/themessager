using System.Security.Claims;

namespace ServicioFrontend.Pages.Shared.Navigation;

public interface INavigationMenuService
{
    NavigationMenuViewModel Build(ClaimsPrincipal user);
}
