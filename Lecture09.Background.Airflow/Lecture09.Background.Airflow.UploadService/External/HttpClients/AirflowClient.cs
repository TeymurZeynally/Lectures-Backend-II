using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lecture09.Background.Airflow.UploadService.External.HttpClients.Models;

namespace Lecture09.Background.Airflow.UploadService.External.HttpClients;

public class AirflowClient : IAirflowClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AirflowClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<DagRunDto> TriggerDagRunAsync(string dagId, string dagRunId, object conf)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v2/dags/{dagId}/dagRuns")
        {
            Content = JsonContent.Create(new { logical_date = (DateTimeOffset?)null, dag_run_id = dagRunId, conf })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync());

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DagRunDto>(JsonOptions))!;
    }

    private async Task<string> GetTokenAsync()
    {
        var response = await _httpClient.PostAsJsonAsync("auth/token", new
        {
            username = _configuration["Airflow:Username"],
            password = _configuration["Airflow:Password"]
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenDto>(JsonOptions))!.AccessToken;
    }
}
