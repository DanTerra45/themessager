using Mercadito.src.categories.data.repository;
using Mercadito.src.categories.domain.repository;
using Mercadito.src.categories.domain.usecases;
using Mercadito.database;
using Mercadito.database.interfaces;
using Mercadito.src.employees.data.repository;
using Mercadito.src.employees.domain.repository;
using Mercadito.src.employees.domain.usecases;
using Mercadito.src.products.data.repository;
using Mercadito.src.products.domain.repository;
using Mercadito.src.products.domain.usecases;

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
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<IDataBaseConnection, MySqlConnectionFactory>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddScoped<IRegisterNewProductWithCategoryUseCase, RegisterNewProductWithCategoryUseCase>();
builder.Services.AddScoped<IUpdateProductUseCase, UpdateProductUseCase>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();

builder.Services.AddScoped<IEmployeeManagementUseCase, EmployeeManagementUseCase>();
builder.Services.AddScoped<IRegisterEmployeeUseCase, RegisterEmployeeUseCase>();
builder.Services.AddScoped<IUpdateEmployeeUseCase, UpdateEmployeeUseCase>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

await app.RunAsync();
