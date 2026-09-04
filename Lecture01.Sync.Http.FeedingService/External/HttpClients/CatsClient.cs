using System.Net.Http.Json;
using Lecture01.Sync.Http.FeedingService.External.HttpClients.Models;

namespace Lecture01.Sync.Http.FeedingService.External.HttpClients;

public class CatsClient : ICatsClient
{
    private readonly HttpClient _httpClient;

    public CatsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CatDto?> GetCatAsync(Guid catId)
    {
        var response = await _httpClient.GetAsync($"cats/{catId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CatDto>();
    }
}
