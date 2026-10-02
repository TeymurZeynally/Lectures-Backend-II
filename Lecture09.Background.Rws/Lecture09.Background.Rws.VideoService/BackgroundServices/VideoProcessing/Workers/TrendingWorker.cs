using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;

namespace Lecture09.Background.Rws.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class TrendingWorker(ILogger<TrendingWorker> logger) : ICronWorker
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Recalculating trending videos for the last hour");
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        logger.LogInformation("Trending videos updated");
    }
}
