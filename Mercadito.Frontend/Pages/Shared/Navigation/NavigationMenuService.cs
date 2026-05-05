using System.Security.Claims;

namespace Mercadito.Frontend.Pages.Shared.Navigation;

public sealed class NavigationMenuService : INavigationMenuService
{
    public NavigationMenuViewModel Build(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var isAuthenticated = user.Identity?.IsAuthenticated == true;
        var isAdmin = user.IsInRole("Admin");
        var isOperator = user.IsInRole("Operador");
        var isAuditor = user.IsInRole("Auditor");

        var items = new List<NavigationTopLevelItem>
        {
            new()
            {
                Label = "Inicio",
                PagePath = "/Index"
            }
        };

        if (isAuthenticated)
        {
            var commercialItems = BuildCommercialItems(isAdmin, isOperator, isAuditor);
            if (commercialItems.Count > 0)
            {
                items.Add(new NavigationTopLevelItem
                {
                    Label = "Comercial",
                    Dropdown = new NavigationDropdownModel
                    {
                        Id = "comercialDropdown",
                        Items = commercialItems
                    }
                });
            }
        }

        items.Add(new NavigationTopLevelItem
        {
            Label = "Productos",
            Dropdown = new NavigationDropdownModel
            {
                Id = "productosDropdown",
                Items = BuildProductItems(isAdmin)
            }
        });

        if (isAdmin)
        {
            items.Add(new NavigationTopLevelItem
            {
                Label = "Categorías",
                Dropdown = new NavigationDropdownModel
                {
                    Id = "categoriasDropdown",
                    Items =
                    [
                        new NavigationDropdownItem
                        {
                            Title = "Organización",
                            Description = "Ver, crear y editar categorías",
                            IconCss = "bi bi-folder",
                            PagePath = "/Categories/Categories"
                        }
                    ]
                }
            });

            items.Add(new NavigationTopLevelItem
            {
                Label = "Empleados",
                Dropdown = new NavigationDropdownModel
                {
                    Id = "empleadosDropdown",
                    AlignEnd = true,
                    Items =
                    [
                        new NavigationDropdownItem
                        {
                            Title = "Personal",
                            Description = "Administración de empleados",
                            IconCss = "bi bi-people",
                            PagePath = "/Employees/Employees"
                        },
                        new NavigationDropdownItem
                        {
                            Title = "Permisos de Acceso",
                            Description = "Usuarios y roles del sistema",
                            IconCss = "bi bi-shield-lock",
                            PagePath = "/Users/Index"
                        }
                    ]
                }
            });
        }

        return new NavigationMenuViewModel
        {
            Items = items,
            Auth = new NavigationAuthViewModel
            {
                IsAuthenticated = isAuthenticated,
                Username = ResolveUsername(user),
                ReturnUrl = "/"
            }
        };
    }

    private static List<NavigationDropdownItem> BuildCommercialItems(bool isAdmin, bool isOperator, bool isAuditor)
    {
        var items = new List<NavigationDropdownItem>();

        if (isAdmin || isOperator)
        {
            items.Add(new NavigationDropdownItem
            {
                Title = "Ventas",
                Description = "Tablero y flujo comercial",
                IconCss = "bi bi-cash-stack",
                PagePath = "/Sales/Index"
            });

            items.Add(new NavigationDropdownItem
            {
                Title = "Anulación",
                Description = "Gestión visual de anulaciones",
                IconCss = "bi bi-receipt-cutoff",
                PagePath = "/Sales/Cancellation"
            });
        }

        if (isAdmin || isAuditor)
        {
            items.Add(new NavigationDropdownItem
            {
                Title = "Reportes",
                Description = "Resumen comercial y exportación",
                IconCss = "bi bi-bar-chart-line",
                PagePath = "/Sales/Reports"
            });
        }

        if (isAdmin)
        {
            items.Add(new NavigationDropdownItem
            {
                Title = "Proveedores",
                Description = "Abastecimiento y contactos",
                IconCss = "bi bi-truck",
                PagePath = "/Suppliers/Suppliers"
            });
        }

        return items;
    }

    private static IReadOnlyList<NavigationDropdownItem> BuildProductItems(bool isAdmin)
    {
        var items = new List<NavigationDropdownItem>();

        if (isAdmin)
        {
            items.Add(new NavigationDropdownItem
            {
                Title = "Gestión de Inventario",
                Description = "Ver, crear y editar productos",
                IconCss = "bi bi-box-seam",
                PagePath = "/Products/Products"
            });
        }

        items.Add(new NavigationDropdownItem
        {
            Title = "Catálogo",
            Description = "Consulta de productos en modo lectura",
            IconCss = "bi bi-grid",
            PagePath = "/Products/Catalog"
        });

        return items;
    }

    private static string ResolveUsername(ClaimsPrincipal user)
    {
        var username = user.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return "Usuario";
        }

        return username;
    }
}
