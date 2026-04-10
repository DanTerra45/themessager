using System.Security.Claims;

namespace Mercadito.Pages.Shared.Navigation
{
    public sealed class NavigationMenuService : INavigationMenuService
    {
        public NavigationMenuViewModel Build(ClaimsPrincipal user, string currentUrl)
        {
            ArgumentNullException.ThrowIfNull(user);

            var currentRole = string.Empty;
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim != null && roleClaim.Value != null)
            {
                currentRole = roleClaim.Value;
            }
            var isAuthenticated = user.Identity?.IsAuthenticated == true;
            var isAdmin = string.Equals(currentRole, "Admin", StringComparison.Ordinal);
            var isOperator = string.Equals(currentRole, "Operador", StringComparison.Ordinal);
            var isAuditor = string.Equals(currentRole, "Auditor", StringComparison.Ordinal);

            var items = new List<NavigationTopLevelItem>
            {
                new()
                {
                    Label = "Inicio",
                    PagePath = "/Index"
                }
            };

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

            items.Add(new NavigationTopLevelItem
            {
                Label = "Productos",
                Dropdown = new NavigationDropdownModel
                {
                    Id = "productosDropdown",
                    Items = BuildProductItems(isAdmin, isOperator)
                }
            });

            var categoryItems = BuildCategoryItems(isAdmin, isAuditor);
            if (categoryItems.Count > 0)
            {
                items.Add(new NavigationTopLevelItem
                {
                    Label = "Categorías",
                    Dropdown = new NavigationDropdownModel
                    {
                        Id = "categoriasDropdown",
                        Items = categoryItems
                    }
                });
            }

            var employeeItems = BuildEmployeeItems(isAdmin);
            if (employeeItems.Count > 0)
            {
                items.Add(new NavigationTopLevelItem
                {
                    Label = "Empleados",
                    Dropdown = new NavigationDropdownModel
                    {
                        Id = "empleadosDropdown",
                        AlignEnd = true,
                        Items = employeeItems
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

        private static string ResolveUsername(ClaimsPrincipal user)
        {
            var username = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Usuario";
            }

            return username;
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
                    PagePath = "/Sales/Sales"
                });
            }

            if (isAdmin || isOperator)
            {
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

            if (isAdmin || isOperator)
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

        private static List<NavigationDropdownItem> BuildProductItems(bool isAdmin, bool isOperator)
        {
            var items = new List<NavigationDropdownItem>();

            if (isAdmin || isOperator)
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

        private static List<NavigationDropdownItem> BuildCategoryItems(bool isAdmin, bool isAuditor)
        {
            var items = new List<NavigationDropdownItem>();

            if (isAdmin)
            {
                items.Add(new NavigationDropdownItem
                {
                    Title = "Organización",
                    Description = "Ver, crear y editar categorías",
                    IconCss = "bi bi-folder",
                    PagePath = "/Categories/Categories"
                });

                items.Add(new NavigationDropdownItem
                {
                    Title = "Árbol de Categorías",
                    Description = "Ver dependencias (Próximamente)",
                    IconCss = "bi bi-diagram-3",
                    Href = "#"
                });
            }

            if (isAdmin || isAuditor)
            {
                items.Add(new NavigationDropdownItem
                {
                    Title = "Consulta",
                    Description = "Vista de categorías en modo lectura",
                    IconCss = "bi bi-eye",
                    PagePath = "/Categories/Browse"
                });
            }

            return items;
        }

        private static IReadOnlyList<NavigationDropdownItem> BuildEmployeeItems(bool isAdmin)
        {
            if (!isAdmin)
            {
                return [];
            }

            return
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
                    Title = "Control de Horarios",
                    Description = "Turnos y asistencias (Próximamente)",
                    IconCss = "bi bi-clock-history",
                    Href = "#"
                },
                new NavigationDropdownItem
                {
                    Title = "Permisos de Acceso",
                    Description = "Usuarios y roles del sistema",
                    IconCss = "bi bi-shield-lock",
                    PagePath = "/Users/Users"
                }
            ];
        }
    }
}

