using System.Diagnostics;
using Lecture03.Replication.App.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lecture03.Replication.App.BackgroundServices;

public sealed class LoadGenerator(
    IDbContextFactory<CatsDbContext> dbContextFactory,
    IOptions<LoadGeneratorOptions> options,
    ILogger<LoadGenerator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        int[] catIds;
        await using (var context = await dbContextFactory.CreateDbContextAsync(stoppingToken))
        {
            catIds = await context.Cats.Select(c => c.Id).ToArrayAsync(stoppingToken);
        }

        logger.LogInformation("Found {CatCount} cats to pet", catIds.Length);

        var workers = Enumerable.Range(0, settings.ParallelWorkers)
            .Select(workerId => RunAsync(workerId, catIds, settings, stoppingToken));

        await Task.WhenAll(workers);
    }

    private async Task RunAsync(int workerId, int[] catIds, LoadGeneratorOptions settings, CancellationToken stoppingToken)
    {
        var random = new Random(workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var catId = catIds[random.Next(catIds.Length)];
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await using var context = await dbContextFactory.CreateDbContextAsync(stoppingToken);
                var cat = await context.Cats.FirstOrDefaultAsync(c => c.Id == catId, stoppingToken);
                if (cat is not null)
                {
                    cat.PetsCount++;
                    cat.LastPetAt = DateTimeOffset.UtcNow;
                    await context.SaveChangesAsync(stoppingToken);

                    logger.LogInformation(
                        "Worker {WorkerId} petted {CatName} (#{CatId}) -> {PetsCount} pets, {ElapsedMs} ms",
                        workerId, cat.Name, catId, cat.PetsCount, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    "Worker {WorkerId} failed to pet cat #{CatId} after {ElapsedMs} ms, retrying",
                    workerId, catId, stopwatch.ElapsedMilliseconds);
            }

            try
            {
                await Task.Delay(settings.UpdateDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
