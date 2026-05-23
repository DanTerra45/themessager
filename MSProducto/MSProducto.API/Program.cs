using MSProducto.API.Middlewares;
using MSProducto.Application.Services;
using MSProducto.Domain.Ports.Input;
using MSProducto.Infrastructure.Extensions;
using MSProducto.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var f = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    DbInitializer.Initialize(f);
}

app.UseCors("AllowAll");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();