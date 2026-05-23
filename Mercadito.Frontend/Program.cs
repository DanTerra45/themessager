using Mercadito.Frontend.Adapters.Employees;
using Mercadito.Frontend.Adapters.Sales;
using Mercadito.Frontend.Adapters.Suppliers;
using Mercadito.Frontend.Adapters.Users;
using Mercadito.Frontend.Authentication;
using Mercadito.Frontend.Pages.Sales;
using Mercadito.Frontend.Pages.Shared.Navigation;
using Mercadito.Frontend.Services;
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
        options.Conventions.AuthorizePage("/Sales/Reports", "SalesViewer");
        options.Conventions.AuthorizePage("/Sales/Receipt");
        options.Conventions.AuthorizePage("/Employees/Employees", "AdminOnly");
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
    client.BaseAddress = new Uri(builder.Configuration["Services:UsersApi"] ?? "http://localhost:5078");
});
builder.Services.AddScoped<ISalesApiAdapter, HttpSalesApiAdapter>();
builder.Services.AddScoped<IEmployeesApiAdapter, HttpEmployeesApiAdapter>();
builder.Services.AddScoped<ISuppliersApiAdapter, HttpSuppliersApiAdapter>();
builder.Services.AddScoped<IUsersApiAdapter, HttpUsersApiAdapter>();
builder.Services.AddScoped<INavigationMenuService, NavigationMenuService>();
builder.Services.AddScoped<IDailyCashClosingExcelExporter, DailyCashClosingExcelExporter>();
builder.Services.AddScoped<ISalesListingExcelExporter, SalesListingExcelExporter>();

// Auth header forwarding handler for MS Producto
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthHeaderHandler>();

// Typed client — MS Producto (products)
builder.Services.AddHttpClient<IProductoApiClient, ProductoApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MicroserviceUrls:Producto"]!);
})
.AddHttpMessageHandler<AuthHeaderHandler>();

// Typed client — MS Producto (categories)
builder.Services.AddHttpClient<ICategoriaApiClient, CategoriaApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MicroserviceUrls:Producto"]!);
})
.AddHttpMessageHandler<AuthHeaderHandler>();

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
