using Mercadito.src.shared.infrastructure.persistence;
using Mercadito.src.application.audit.ports.input;
using Mercadito.src.application.audit.ports.output;
using Mercadito.src.application.audit.services;
using Mercadito.src.application.audit.usecases;
using Mercadito.src.infrastructure.audit.persistence;
using Mercadito.src.application.categories.ports.input;
using Mercadito.src.application.categories.ports.output;
using Mercadito.src.application.categories.usecases;
using Mercadito.src.application.categories.validation;
using Mercadito.src.domain.categories.factories;
using Mercadito.src.infrastructure.categories.persistence;
using Mercadito.src.application.notifications.ports.output;
using Mercadito.src.infrastructure.notifications.background;
using Mercadito.src.infrastructure.notifications.email;
using Mercadito.src.infrastructure.notifications.options;
using Mercadito.src.infrastructure.notifications.persistence;
using Mercadito.src.application.employees.ports.input;
using Mercadito.src.application.employees.ports.output;
using Mercadito.src.application.employees.usecases;
using Mercadito.src.application.employees.validation;
using Mercadito.src.domain.employees.factories;
using Mercadito.src.infrastructure.employees.persistence;
using Mercadito.src.application.products.ports.input;
using Mercadito.src.application.products.ports.output;
using Mercadito.src.application.products.usecases;
using Mercadito.src.application.products.validation;
using Mercadito.src.domain.products.factories;
using Mercadito.src.infrastructure.products.persistence;
using Mercadito.src.domain.shared.validation;
using Mercadito.src.application.sales.facades;
using Mercadito.src.application.sales.ports.input;
using Mercadito.src.application.sales.ports.output;
using Mercadito.src.application.sales.validation;
using Mercadito.src.infrastructure.sales.persistence;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.application.usecases;
using Mercadito.src.suppliers.application.validation;
using Mercadito.src.domain.suppliers.factories;
using Mercadito.src.infrastructure.suppliers.persistence;
using Mercadito.src.application.users.ports.input;
using Mercadito.src.application.users.ports.output;
using Mercadito.src.application.users.usecases;
using Mercadito.src.application.users.validation;
using Mercadito.src.application.users;
using Mercadito.src.infrastructure.users.persistence;
using Mercadito.src.infrastructure.users.security;
using Mercadito.Pages.Infrastructure;
using Mercadito.Pages.Shared.Navigation;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var culture = new CultureInfo("es-BO");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Products/Products", "/Products/{handler?}");
    options.Conventions.AddPageRoute("/Products/Catalog", "/Catalog/{handler?}");
    options.Conventions.AddPageRoute("/Categories/Categories", "/Categories/{handler?}");
    options.Conventions.AddPageRoute("/Categories/Browse", "/CategoryCatalog/{handler?}");
    options.Conventions.AddPageRoute("/Employees/Employees", "/Employees/{handler?}");
    options.Conventions.AddPageRoute("/Suppliers/Suppliers", "/Suppliers/{handler?}");
    options.Conventions.AddPageRoute("/Users/Users", "/Users/{handler?}");
    options.Conventions.AuthorizePage("/Products/Products", "OperatorOrAdmin");
    options.Conventions.AuthorizePage("/Categories/Categories", "AdminOnly");
    options.Conventions.AuthorizePage("/Categories/Browse", "AuditorOrAdmin");
    options.Conventions.AuthorizePage("/Employees/Employees", "AdminOnly");
    options.Conventions.AuthorizePage("/Suppliers/Suppliers", "OperatorOrAdmin");
    options.Conventions.AuthorizePage("/Users/Users", "AdminOnly");
    options.Conventions.AddPageRoute("/Account/Login", "/Login");
    options.Conventions.AddPageRoute("/Account/ForgotPassword", "/ForgotPassword");
    options.Conventions.AddPageRoute("/Account/ResetPassword", "/ResetPassword/{token?}");
    options.Conventions.AddPageRoute("/Account/ChangePassword", "/ChangePassword");
    options.Conventions.AddPageRoute("/Sales/Sales", "/Sales/{handler?}");
    options.Conventions.AddPageRoute("/Sales/Cancellation", "/Sales/Cancellation/{handler?}");
    options.Conventions.AddPageRoute("/Sales/Reports", "/Sales/Reports/{handler?}");
    options.Conventions.AddPageRoute("/Sales/Receipt", "/Sales/Receipt/{saleId?}");
    options.Conventions.AuthorizePage("/Sales/Sales", "OperatorOrAdmin");
    options.Conventions.AuthorizePage("/Sales/Cancellation", "OperatorOrAdmin");
    options.Conventions.AuthorizePage("/Sales/Reports", "AuditorOrAdmin");
    options.Conventions.AuthorizePage("/Sales/Receipt");
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
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
    .AddPolicy("AuditorOrAdmin", policy => policy.RequireRole("Admin", "Auditor"));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection("EmailOutbox"));

builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddSingleton<IListingPageStateService, ListingPageStateService>();
builder.Services.AddSingleton<IModalPostbackStateService, ModalPostbackStateService>();
builder.Services.AddHostedService<EmailOutboxDispatcher>();

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<IProductRepository>(serviceProvider => serviceProvider.GetRequiredService<ProductRepository>());

builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ICategoryRepository>(serviceProvider => serviceProvider.GetRequiredService<CategoryRepository>());
builder.Services.AddScoped<IProductCategoryLookupRepository>(serviceProvider => serviceProvider.GetRequiredService<CategoryRepository>());

builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<IEmployeeRepository>(serviceProvider => serviceProvider.GetRequiredService<EmployeeRepository>());

builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<ISupplierRepository>(serviceProvider => serviceProvider.GetRequiredService<SupplierRepository>());

builder.Services.AddScoped<SalesRepository>();
builder.Services.AddScoped<ISalesRepository>(serviceProvider => serviceProvider.GetRequiredService<SalesRepository>());
builder.Services.AddScoped<ISalesQueryFacade, SalesQueryFacade>();
builder.Services.AddScoped<IRegisterSaleFacade, RegisterSaleFacade>();
builder.Services.AddScoped<IUpdateSaleFacade, UpdateSaleFacade>();
builder.Services.AddScoped<ICancelSaleFacade, CancelSaleFacade>();

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IUserAccessWorkflowRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
builder.Services.AddScoped<EmailOutboxRepository>();
builder.Services.AddScoped<IEmailOutboxRepository>(serviceProvider => serviceProvider.GetRequiredService<EmailOutboxRepository>());
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<IAuditRepository>(serviceProvider => serviceProvider.GetRequiredService<AuditRepository>());
builder.Services.AddScoped<IAuditTrailService, AuditTrailService>();

builder.Services.AddSingleton<IProductFactory, ProductFactory>();
builder.Services.AddSingleton<ICategoryFactory, CategoryFactory>();
builder.Services.AddSingleton<IEmployeeFactory, EmployeeFactory>();
builder.Services.AddSingleton<ISupplierFactory, SupplierFactory>();
builder.Services.AddSingleton<SodiumPasswordService>();
builder.Services.AddSingleton<IPasswordHasher>(serviceProvider => serviceProvider.GetRequiredService<SodiumPasswordService>());
builder.Services.AddSingleton<IPasswordVerifier>(serviceProvider => serviceProvider.GetRequiredService<SodiumPasswordService>());

builder.Services.AddTransient<IValidator<CreateSupplierDto, SupplierDto>, CreateSupplierValidator>();
builder.Services.AddTransient<IValidator<UpdateSupplierDto, SupplierDto>, UpdateSupplierValidator>();
builder.Services.AddTransient<ISupplierFormHintsProvider>(serviceProvider =>
    (ISupplierFormHintsProvider)serviceProvider.GetRequiredService<IValidator<CreateSupplierDto, SupplierDto>>());
builder.Services.AddTransient<ICreateProductValidator, CreateProductValidator>();
builder.Services.AddTransient<IUpdateProductValidator, UpdateProductValidator>();
builder.Services.AddTransient<ICreateCategoryValidator, CreateCategoryValidator>();
builder.Services.AddTransient<IUpdateCategoryValidator, UpdateCategoryValidator>();
builder.Services.AddTransient<ICreateEmployeeValidator, CreateEmployeeValidator>();
builder.Services.AddTransient<IUpdateEmployeeValidator, UpdateEmployeeValidator>();
builder.Services.AddTransient<ICreateUserValidator, CreateUserValidator>();
builder.Services.AddTransient<IAssignTemporaryPasswordValidator, AssignTemporaryPasswordValidator>();
builder.Services.AddTransient<ISendAdministrativePasswordResetLinkValidator, SendAdministrativePasswordResetLinkValidator>();
builder.Services.AddTransient<IForcePasswordChangeValidator, ForcePasswordChangeValidator>();
builder.Services.AddTransient<ILoginUserValidator, LoginUserValidator>();
builder.Services.AddTransient<IRequestPasswordResetValidator, RequestPasswordResetValidator>();
builder.Services.AddTransient<ICompletePasswordResetValidator, CompletePasswordResetValidator>();
builder.Services.AddTransient<IRegisterSaleValidator, RegisterSaleValidator>();
builder.Services.AddTransient<IUpdateSaleValidator, UpdateSaleValidator>();
builder.Services.AddTransient<ICancelSaleValidator, CancelSaleValidator>();

builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();
builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();
builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<IRegisterAuditEntryUseCase, RegisterAuditEntryUseCase>();
builder.Services.AddScoped<IRequestPasswordResetUseCase, RequestPasswordResetUseCase>();
builder.Services.AddScoped<IValidatePasswordResetTokenUseCase, ValidatePasswordResetTokenUseCase>();
builder.Services.AddScoped<ICompletePasswordResetUseCase, CompletePasswordResetUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ISendAdministrativePasswordResetLinkUseCase, SendAdministrativePasswordResetLinkUseCase>();
builder.Services.AddScoped<IAssignTemporaryPasswordUseCase, AssignTemporaryPasswordUseCase>();
builder.Services.AddScoped<IForcePasswordChangeUseCase, ForcePasswordChangeUseCase>();
builder.Services.AddScoped<IDeactivateUserUseCase, DeactivateUserUseCase>();
builder.Services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();
builder.Services.AddScoped<IGetAvailableEmployeesUseCase, GetAvailableEmployeesUseCase>();

builder.Services.AddScoped<IRegisterSupplierUseCase, RegisterSupplierUseCase>();
builder.Services.AddScoped<IGetAllSuppliersUseCase, GetAllSuppliersUseCase>();
builder.Services.AddScoped<IGetSupplierByIdUseCase, GetSupplierByIdUseCase>();
builder.Services.AddScoped<IGetNextSupplierCodeUseCase, GetNextSupplierCodeUseCase>();
builder.Services.AddScoped<IUpdateSupplierUseCase, UpdateSupplierUseCase>();
builder.Services.AddScoped<IDeleteSupplierUseCase, DeleteSupplierUseCase>();
builder.Services.AddScoped<INavigationMenuService, NavigationMenuService>();

var app = builder.Build();

app.UseExceptionHandler("/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

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

await app.RunAsync();

static bool RequiresForcedPasswordChangeRedirect(HttpContext context)
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        return false;
    }

    var mustChangePasswordClaim = context.User.FindFirst(UserClaimTypes.MustChangePassword);
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
