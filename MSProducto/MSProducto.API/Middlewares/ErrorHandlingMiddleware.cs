namespace MSProducto.API.Middlewares;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = "Error interno del servidor." });
        }
    }
}