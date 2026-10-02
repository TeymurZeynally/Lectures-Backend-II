using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;

namespace Lecture09.Background.Rws.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class UploadsPollingWorker(ILogger<UploadsPollingWorker> logger) : IRecurrentWorker
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Polling videos with status 'uploaded'");
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        logger.LogInformation("Found {Count} uploaded videos, status set to 'validated'", Random.Shared.Next(0, 4));
    }
}
