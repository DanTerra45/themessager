using ServicioFrontend.Adapters.Categories;
using ServicioFrontend.Adapters.Employees;
using ServicioFrontend.Adapters.Products;
using ServicioFrontend.Adapters.Sales;
using ServicioFrontend.Adapters.Suppliers;
using ServicioFrontend.Adapters.Users;
using ServicioFrontend.Authentication;
using ServicioFrontend.Pages.Sales;
using ServicioFrontend.Pages.Shared.Navigation;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
var salesEnabled = builder.Configuration.GetValue("Features:SalesEnabled", true);

builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AddPageRoute("/Account/ResetPassword", "/reset-password");

        if (salesEnabled)
        {
            options.Conventions.AuthorizePage("/Sales/Index", "OperatorOrAdmin");
            options.Conventions.AuthorizePage("/Sales/Create", "OperatorOrAdmin");
            options.Conventions.AuthorizePage("/Sales/Cancellation", "OperatorOrAdmin");
            options.Conventions.AuthorizePage("/Sales/Cancel", "OperatorOrAdmin");
            options.Conventions.AuthorizePage("/Sales/Detail", "SalesViewer");
            options.Conventions.AuthorizePage("/Sales/Reports", "SalesViewer");
            options.Conventions.AuthorizePage("/Sales/Receipt");
        }

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
    .AddPolicy("OperatorOrAdmin", policy => policy.RequireRole("Admin", "Operator", "Operador"))
    .AddPolicy("AuditorOrAdmin", policy => policy.RequireRole("Admin", "Auditor"))
    .AddPolicy("SalesViewer", policy => policy.RequireRole("Admin", "Operator", "Operador", "Auditor"));

builder.Services.AddHttpContextAccessor();

if (salesEnabled)
{
    builder.Services.AddHttpClient("SalesApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:SalesApi"] ?? "http://localhost:5161");
    });
    builder.Services.AddHttpClient("ReportsApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:ReportsApi"] ?? "http://localhost:5311");
    });
}
builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"] ?? "http://localhost:5150");
});
builder.Services.AddHttpClient("EmployeesApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:EmployeesApi"] ?? "http://localhost:5200");
});
builder.Services.AddHttpClient("SuppliersApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:SuppliersApi"] ?? "http://localhost:5250");
});
builder.Services.AddHttpClient("UsersApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UsersApi"] ?? "http://localhost:5078");
});
if (salesEnabled)
{
    builder.Services.AddScoped<ISalesApiAdapter, HttpSalesApiAdapter>();
}
builder.Services.AddScoped<ICategoriesApiAdapter, HttpCategoriesApiAdapter>();
builder.Services.AddScoped<IEmployeesApiAdapter, HttpEmployeesApiAdapter>();
builder.Services.AddScoped<IProductsApiAdapter, HttpProductsApiAdapter>();
builder.Services.AddScoped<ISuppliersApiAdapter, HttpSuppliersApiAdapter>();
builder.Services.AddScoped<IUsersApiAdapter, HttpUsersApiAdapter>();
builder.Services.AddScoped<INavigationMenuService, NavigationMenuService>();
if (salesEnabled)
{
    builder.Services.AddScoped<IDailyCashClosingExcelExporter, DailyCashClosingExcelExporter>();
    builder.Services.AddScoped<ISalesListingExcelExporter, SalesListingExcelExporter>();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
if (!salesEnabled)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/Sales", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next();
    });
}
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
