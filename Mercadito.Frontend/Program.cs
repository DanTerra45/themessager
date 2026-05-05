using Mercadito.Frontend.Adapters.Categories;
using Mercadito.Frontend.Adapters.Employees;
using Mercadito.Frontend.Adapters.Products;
using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Adapters.Suppliers;
using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Authentication;
using Mercadito.Frontend.Pages.Shared.Navigation;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizePage("/Sales/Index", "OperatorOrAdmin");
        options.Conventions.AuthorizePage("/Sales/Create", "OperatorOrAdmin");
        options.Conventions.AuthorizePage("/Sales/Cancellation", "OperatorOrAdmin");
        options.Conventions.AuthorizePage("/Sales/Cancel", "OperatorOrAdmin");
        options.Conventions.AuthorizePage("/Sales/Detail", "SalesViewer");
        options.Conventions.AuthorizePage("/Sales/Reports", "AuditorOrAdmin");
        options.Conventions.AuthorizePage("/Sales/Receipt");
        options.Conventions.AuthorizePage("/Categories/Categories", "AdminOnly");
        options.Conventions.AuthorizePage("/Employees/Employees", "AdminOnly");
        options.Conventions.AuthorizePage("/Products/Products", "AdminOnly");
        options.Conventions.AuthorizePage("/Suppliers/Suppliers", "AdminOnly");
        options.Conventions.AuthorizePage("/Users/Index", "AdminOnly");
        options.Conventions.AuthorizePage("/Account/ChangePassword");
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("OperatorOrAdmin", policy => policy.RequireRole("Admin", "Operador"))
    .AddPolicy("AuditorOrAdmin", policy => policy.RequireRole("Admin", "Auditor"))
    .AddPolicy("SalesViewer", policy => policy.RequireRole("Admin", "Operador", "Auditor"));

builder.Services.AddHttpClient("SalesApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:SalesApi"] ?? "http://localhost:5101");
});
builder.Services.AddHttpClient("UsersApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UsersApi"] ?? "http://localhost:5102");
});
builder.Services.AddScoped<ISalesApiAdapter, HttpSalesApiAdapter>();
builder.Services.AddScoped<ICategoriesApiAdapter, HttpCategoriesApiAdapter>();
builder.Services.AddScoped<IEmployeesApiAdapter, HttpEmployeesApiAdapter>();
builder.Services.AddScoped<IProductsApiAdapter, HttpProductsApiAdapter>();
builder.Services.AddScoped<ISuppliersApiAdapter, HttpSuppliersApiAdapter>();
builder.Services.AddScoped<IUsersApiAdapter, HttpUsersApiAdapter>();
builder.Services.AddScoped<INavigationMenuService, NavigationMenuService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (RequiresForcedPasswordChangeRedirect(context))
    {
        context.Response.Redirect("/ChangePassword");
        return;
    }

    await next();
});
app.UseAuthorization();
app.MapRazorPages();

app.Run();

static bool RequiresForcedPasswordChangeRedirect(HttpContext context)
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        return false;
    }

    var mustChangePasswordClaim = context.User.FindFirst(FrontendUserClaimTypes.MustChangePassword);
    if (mustChangePasswordClaim == null)
    {
        return false;
    }

    if (!string.Equals(mustChangePasswordClaim.Value, "true", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var requestPath = context.Request.Path;
    if (requestPath.StartsWithSegments("/ChangePassword") || requestPath.StartsWithSegments("/Account/ChangePassword"))
    {
        return false;
    }

    if (requestPath.StartsWithSegments("/Login") || requestPath.StartsWithSegments("/Account/Login"))
    {
        return false;
    }

    return true;
}
