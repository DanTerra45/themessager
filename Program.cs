using Mercadito;

using System.Globalization;

var culture = new CultureInfo("es-BO");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Database connection
var dbProvider = builder.Configuration["Database:Provider"] ?? "MySQL";

switch (dbProvider)
{
    case "MySQL":
        builder.Services.AddScoped<IDataBaseConnection, MySqlConnectionFactory>();
        break;
    default:
        throw new InvalidOperationException($"Proveedor de base de datos no soportado: {dbProvider}");
}

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();

// Use Cases
builder.Services.AddScoped<AsignCategoryToProductUseCase>();
builder.Services.AddScoped<RegisterNewProductUseCase>();
builder.Services.AddScoped<RegisterNewProductWithCategoryUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
