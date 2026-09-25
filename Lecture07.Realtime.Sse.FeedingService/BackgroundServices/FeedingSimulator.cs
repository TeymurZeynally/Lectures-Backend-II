using Lecture07.Realtime.Sse.FeedingService.Api.Feeders.Services;

namespace Lecture07.Realtime.Sse.FeedingService.BackgroundServices;

// Pretends to be smart feeders in a shelter reporting how much food is left
public sealed class FeedingSimulator(
    IFeedersService feedersService,
    ILogger<FeedingSimulator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();
        var feeders = feedersService.GetAll();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var feeder = feeders[random.Next(feeders.Count)];
            var updated = feedersService.Eat(feeder.Id, random.Next(5, 30))!;

            logger.LogInformation("{CatName} ate, {FoodGrams} g left", updated.CatName, updated.FoodGrams);
        }
    }
}
