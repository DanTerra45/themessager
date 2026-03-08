using Mercadito.src.categories.data.repository;
using Mercadito.src.categories.domain.repository;
using Mercadito.src.categories.domain.usecases;
using Mercadito.database;
using Mercadito.database.interfaces;
using Mercadito.src.products.data.repository;
using Mercadito.src.products.domain.repository;
using Mercadito.src.products.domain.usecases;

using System.Globalization;

var culture = new CultureInfo("es-BO");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

builder.Services.AddScoped<IDataBaseConnection, MySqlConnectionFactory>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<IProductManagementUseCase, ProductManagementUseCase>();
builder.Services.AddScoped<IRegisterNewProductWithCategoryUseCase, RegisterNewProductWithCategoryUseCase>();
builder.Services.AddScoped<IUpdateProductUseCase, UpdateProductUseCase>();
builder.Services.AddScoped<ICategoryManagementUseCase, CategoryManagementUseCase>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
