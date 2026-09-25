using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture08.LoadBalancing.LoadGenerator.BackgroundServices;

public sealed class LoadGenerator(
    HttpClient httpClient,
    IOptions<LoadGeneratorOptions> options,
    ILogger<LoadGenerator> logger) : BackgroundService
{
    // instance name -> responses and their total time for the current second
    private Dictionary<string, (int Count, long TotalMs)> _responses = new();
    private readonly Lock _lock = new();
    private int _errors;
    private int _inFlight;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        logger.LogInformation("Sending {Rate} req/s to {Url}", settings.RequestsPerSecond, settings.Url);

        _ = ReportAsync(stoppingToken);

        // Requests are sent in 10 small portions per second and nobody waits for the responses:
        // the rate stays the same no matter how slow the instances are
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        var budget = 0.0;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            for (budget += settings.RequestsPerSecond / 10.0; budget >= 1; budget--)
            {
                _ = SendAsync(settings.Url, stoppingToken);
            }
        }
    }

    private async Task SendAsync(string url, CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref _inFlight);
        var stopwatch = Stopwatch.StartNew();

        try
        {
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
