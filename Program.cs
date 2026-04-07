using Mercadito.database;
using Mercadito.database.interfaces;
using Mercadito.src.audit.application.ports.input;
using Mercadito.src.audit.application.ports.output;
using Mercadito.src.audit.application.services;
using Mercadito.src.audit.application.use_cases;
using Mercadito.src.audit.infrastructure.persistence;
using Mercadito.src.categories.application.ports.input;
using Mercadito.src.categories.application.ports.output;
using Mercadito.src.categories.application.use_cases;
using Mercadito.src.categories.application.validation;
using Mercadito.src.categories.domain.factories;
using Mercadito.src.categories.infrastructure.persistence;
using Mercadito.src.notifications.application.ports.output;
using Mercadito.src.notifications.infrastructure.background;
using Mercadito.src.notifications.infrastructure.email;
using Mercadito.src.notifications.infrastructure.options;
using Mercadito.src.notifications.infrastructure.persistence;
using Mercadito.src.employees.application.ports.input;
using Mercadito.src.employees.application.ports.output;
using Mercadito.src.employees.application.use_cases;
using Mercadito.src.employees.application.validation;
using Mercadito.src.employees.domain.factories;
using Mercadito.src.employees.infrastructure.persistence;
using Mercadito.src.products.application.ports.input;
using Mercadito.src.products.application.ports.output;
using Mercadito.src.products.application.use_cases;
using Mercadito.src.products.application.validation;
using Mercadito.src.products.domain.factories;
using Mercadito.src.products.infrastructure.persistence;
using Mercadito.src.shared.domain.validator;
using Mercadito.src.suppliers.application.models;
using Mercadito.src.suppliers.application.ports.input;
using Mercadito.src.suppliers.application.ports.output;
using Mercadito.src.suppliers.application.use_cases;
using Mercadito.src.suppliers.application.validation;
using Mercadito.src.suppliers.domain.factories;
using Mercadito.src.suppliers.infrastructure.persistence;
using Mercadito.src.users.application.ports.input;
using Mercadito.src.users.application.ports.output;
using Mercadito.src.users.application.use_cases;
using Mercadito.src.users.application.validation;
using Mercadito.src.users.infrastructure.persistence;
using Mercadito.src.users.infrastructure.security;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var culture = new CultureInfo("es-BO");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Products/Products", "/Products/{handler?}");
    options.Conventions.AddPageRoute("/Categories/Categories", "/Categories/{handler?}");
    options.Conventions.AddPageRoute("/Employees/Employees", "/Employees/{handler?}");
    options.Conventions.AddPageRoute("/Suppliers/Suppliers", "/Suppliers/{handler?}");
    options.Conventions.AddPageRoute("/Users/Users", "/Users/{handler?}");
    options.Conventions.AuthorizePage("/Users/Users", "AdminOnly");
    options.Conventions.AddPageRoute("/Account/Login", "/Login");
    options.Conventions.AddPageRoute("/Account/ForgotPassword", "/ForgotPassword");
    options.Conventions.AddPageRoute("/Account/ResetPassword", "/ResetPassword/{token?}");
    options.Conventions.AddPageRoute("/Sales/Sales", "/Sales/{handler?}");
    options.Conventions.AddPageRoute("/Sales/Cancellation", "/Sales/Cancellation/{handler?}");
    options.Conventions.AddPageRoute("/Sales/Reports", "/Sales/Reports/{handler?}");
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
        options.AccessDeniedPath = "/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection("EmailOutbox"));

builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
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

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(serviceProvider => serviceProvider.GetRequiredService<UserRepository>());
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

builder.Services.AddSingleton<IValidator<CreateSupplierDto, SupplierDto>, CreateSupplierValidator>();
builder.Services.AddSingleton<IValidator<UpdateSupplierDto, SupplierDto>, UpdateSupplierValidator>();
builder.Services.AddSingleton<ICreateProductValidator, CreateProductValidator>();
builder.Services.AddSingleton<IUpdateProductValidator, UpdateProductValidator>();
builder.Services.AddSingleton<ICreateCategoryValidator, CreateCategoryValidator>();
builder.Services.AddSingleton<IUpdateCategoryValidator, UpdateCategoryValidator>();
builder.Services.AddSingleton<ICreateEmployeeValidator, CreateEmployeeValidator>();
builder.Services.AddSingleton<IUpdateEmployeeValidator, UpdateEmployeeValidator>();
builder.Services.AddSingleton<ICreateUserValidator, CreateUserValidator>();
builder.Services.AddSingleton<IResetUserPasswordValidator, ResetUserPasswordValidator>();
builder.Services.AddSingleton<ILoginUserValidator, LoginUserValidator>();
builder.Services.AddSingleton<IRequestPasswordResetValidator, RequestPasswordResetValidator>();
builder.Services.AddSingleton<ICompletePasswordResetValidator, CompletePasswordResetValidator>();

builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();
builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();
builder.Services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
builder.Services.AddScoped<IRegisterAuditEntryUseCase, RegisterAuditEntryUseCase>();
builder.Services.AddScoped<IRequestPasswordResetUseCase, RequestPasswordResetUseCase>();
builder.Services.AddScoped<IValidatePasswordResetTokenUseCase, ValidatePasswordResetTokenUseCase>();
builder.Services.AddScoped<ICompletePasswordResetUseCase, CompletePasswordResetUseCase>();
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<IResetUserPasswordUseCase, ResetUserPasswordUseCase>();
builder.Services.AddScoped<IDeactivateUserUseCase, DeactivateUserUseCase>();
builder.Services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();
builder.Services.AddScoped<IGetAvailableEmployeesUseCase, GetAvailableEmployeesUseCase>();

builder.Services.AddScoped<IRegisterSupplierUseCase, RegisterSupplierUseCase>();
builder.Services.AddScoped<IGetAllSuppliersUseCase, GetAllSuppliersUseCase>();
builder.Services.AddScoped<IGetSupplierByIdUseCase, GetSupplierByIdUseCase>();
builder.Services.AddScoped<IGetNextSupplierCodeUseCase, GetNextSupplierCodeUseCase>();
builder.Services.AddScoped<IUpdateSupplierUseCase, UpdateSupplierUseCase>();
builder.Services.AddScoped<IDeleteSupplierUseCase, DeleteSupplierUseCase>();

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
app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();
