namespace Mercadito.Frontend.Services;

using System.Net.Http.Json;
using Mercadito.Frontend.Dtos.Categories;

public class CategoriaApiClient : ICategoriaApiClient
{
    private readonly HttpClient _http;

    public CategoriaApiClient(HttpClient http) => _http = http;

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var ms = await _http.GetFromJsonAsync<List<MsCategoriaResponse>>("/api/categoria")
                 ?? new List<MsCategoriaResponse>();
        return ms.Select(CategoriaMapper.ToCategoryDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(long id)
    {
        var ms = await _http.GetFromJsonAsync<MsCategoriaResponse>($"/api/categoria/{id}");
        return ms is null ? null : CategoriaMapper.ToCategoryDto(ms);
    }

    public async Task<(bool Success, string? Error)> CreateCategoryAsync(SaveCategoryRequestDto request)
    {
        var msRequest = CategoriaMapper.ToMsRequest(request);
        var r = await _http.PostAsJsonAsync("/api/categoria", msRequest);
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> UpdateCategoryAsync(long id, SaveCategoryRequestDto request)
    {
        var msRequest = CategoriaMapper.ToMsRequest(request);
        var r = await _http.PutAsJsonAsync($"/api/categoria/{id}", msRequest);
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(long id)
    {
        var r = await _http.DeleteAsync($"/api/categoria/{id}");
        if (r.IsSuccessStatusCode) return (true, null);
        return (false, await r.Content.ReadAsStringAsync());
    }
}