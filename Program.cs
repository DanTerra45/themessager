using Mercadito.src.categories.data.repository;
using Mercadito.src.categories.domain.factory;
using Mercadito.src.categories.domain.usecases;
using Mercadito.database;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.factory;
using Mercadito.src.employees.domain.usecases;
using Mercadito.src.products.data.repository;
using Mercadito.src.products.domain.factory;
using Mercadito.src.products.domain.usecases;
using Mercadito.src.shared.domain.factory;

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

builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<RepositoryCreator<ProductRepository>, ProductRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<CategoryRepository>, CategoryRepositoryCreator>();
builder.Services.AddScoped<RepositoryCreator<EmployeeRepository>, EmployeeRepositoryCreator>();
builder.Services.AddSingleton<IProductFactory, ProductFactory>();
builder.Services.AddSingleton<ICategoryFactory, CategoryFactory>();
builder.Services.AddSingleton<IEmployeeFactory, EmployeeFactory>();

builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();

builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();

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

app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();
