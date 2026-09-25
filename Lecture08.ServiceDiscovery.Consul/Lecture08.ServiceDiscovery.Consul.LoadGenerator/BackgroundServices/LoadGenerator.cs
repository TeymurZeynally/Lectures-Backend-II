using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture08.ServiceDiscovery.Consul.LoadGenerator.BackgroundServices;

public sealed class LoadGenerator(
    HttpClient httpClient,
    IOptions<LoadGeneratorOptions> options,
    ILogger<LoadGenerator> logger) : BackgroundService
{
    private Dictionary<string, (int Count, long TotalMs)> _responses = new();
    private readonly Lock _lock = new();
    private int _errors;
    private int _inFlight;
    private string[] _urls = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        logger.LogInformation("Sending {Rate} req/s ({Mode})", settings.RequestsPerSecond, settings.Mode);

        _ = ReportAsync(stoppingToken);

        if (settings.Mode == "ClientSide")
        {
            _ = DiscoverAsync(settings.ConsulUrl, stoppingToken);
        }
        else
        {
            _urls = [settings.Url];
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        var budget = 0.0;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            for (budget += settings.RequestsPerSecond / 10.0; budget >= 1; budget--)
            {
                _ = SendAsync(stoppingToken);
            }
        }
    }

    private async Task SendAsync(CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref _inFlight);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var urls = _urls;
            var url = urls[Random.Shared.Next(urls.Length)];

            using var response = await httpClient.GetAsync(url, stoppingToken);
            response.EnsureSuccessStatusCode();

            var instance = response.Headers.GetValues("X-Instance").First();
            lock (_lock)
            {
                var (count, totalMs) = _responses.GetValueOrDefault(instance);
                _responses[instance] = (count + 1, totalMs + stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception) when (!stoppingToken.IsCancellationRequested)
        {
            Interlocked.Increment(ref _errors);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    private async Task DiscoverAsync(string consulUrl, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        do
        {
            var entries = await httpClient.GetFromJsonAsync<JsonElement>(
                $"{consulUrl}/v1/health/service/cats?passing", stoppingToken);

            _urls = entries.EnumerateArray()
                .Select(e => e.GetProperty("Service"))
                .Select(s => $"http://{s.GetProperty("Address")}:{s.GetProperty("Port")}/cats/1")
                .ToArray();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReportAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            Dictionary<string, (int Count, long TotalMs)> responses;
            lock (_lock)
            {
                responses = _responses;
                _responses = new();
            }

            var instances = string.Join(" | ", responses
                .OrderBy(r => r.Key)
                .Select(r => $"{r.Key}: {r.Value.Count} req, {r.Value.TotalMs / r.Value.Count} ms"));

            logger.LogInformation(
                "{Instances} | errors: {Errors} | in flight: {InFlight}",
                instances, Interlocked.Exchange(ref _errors, 0), _inFlight);
        }
    }
}
