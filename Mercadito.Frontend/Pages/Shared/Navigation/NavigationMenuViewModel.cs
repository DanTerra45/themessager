namespace Mercadito.Frontend.Pages.Shared.Navigation;

public sealed class NavigationMenuViewModel
{
    public IReadOnlyList<NavigationTopLevelItem> Items { get; init; } = [];
    public NavigationAuthViewModel Auth { get; init; } = new();
}

public sealed class NavigationTopLevelItem
{
    public string Label { get; init; } = string.Empty;
    public string? PagePath { get; init; }
    public NavigationDropdownModel? Dropdown { get; init; }
}

public sealed class NavigationDropdownModel
{
    public string Id { get; init; } = string.Empty;
    public bool AlignEnd { get; init; }
    public IReadOnlyList<NavigationDropdownItem> Items { get; init; } = [];
}

public sealed class NavigationDropdownItem
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconCss { get; init; } = string.Empty;
    public string? PagePath { get; init; }
    public string? Href { get; init; }
}

public sealed class NavigationAuthViewModel
{
    public bool IsAuthenticated { get; init; }
    public string Username { get; init; } = "Usuario";
    public string LoginPath { get; init; } = "/Login";
    public string LogoutPagePath { get; init; } = "/Account/Login";
    public string ReturnUrl { get; init; } = "/";
}
