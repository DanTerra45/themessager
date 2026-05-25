namespace Mercadito.Frontend.Services;

using System.Net.Http.Json;
using Mercadito.Frontend.Dtos.Products;

public class ProductoApiClient : IProductoApiClient
{
    private readonly HttpClient _http;

    public ProductoApiClient(HttpClient http) => _http = http;

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        var ms = await _http.GetFromJsonAsync<List<MsProductoResponse>>("/api/products")
                 ?? new List<MsProductoResponse>();
        return ms.Select(ProductoMapper.ToProductDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(long id)
    {
        var ms = await _http.GetFromJsonAsync<MsProductoResponse>($"/api/products/{id}");
        return ms is null ? null : ProductoMapper.ToProductDto(ms);
    }

    public async Task<(bool Success, string? Error)> CreateProductAsync(SaveProductRequestDto request)
    {
        var msRequest = ProductoMapper.ToMsRequest(request);
        var r = await _http.PostAsJsonAsync("/api/products", msRequest);
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> UpdateProductAsync(long id, SaveProductRequestDto request)
    {
        var msRequest = ProductoMapper.ToMsRequest(request);
        var r = await _http.PutAsJsonAsync($"/api/products/{id}", msRequest);
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> DeleteProductAsync(long id)
    {
        var r = await _http.DeleteAsync($"/api/products/{id}");
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }
}