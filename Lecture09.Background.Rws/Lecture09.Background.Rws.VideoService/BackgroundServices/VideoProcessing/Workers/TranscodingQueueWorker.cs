using Microsoft.Extensions.Logging;
using RecurrentWorkerService.Workers;
using RecurrentWorkerService.Workers.Models;

namespace Lecture09.Background.Rws.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class TranscodingQueueWorker(ILogger<TranscodingQueueWorker> logger) : IWorkloadWorker
{
    public async Task<Workload> ExecuteAsync(CancellationToken cancellationToken)
    {
        int[] loadProfile = [0, 0, 30, 100, 100, 60, 10];
        var phase = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 20 % loadProfile.Length;
        var waiting = (int)Math.Round(10 * loadProfile[phase] / 100.0 * (0.8 + 0.4 * Random.Shared.NextDouble()));
        var taken = Math.Min(waiting, 10);

        await Task.Delay(TimeSpan.FromMilliseconds(100 + 20 * taken), cancellationToken);

        var workload = Workload.FromDoneItems(taken, 10);

        logger.LogInformation("Transcoding queue: took {Taken} of 10 videos (workload {Workload}/255)", taken, workload.Value);

        return workload;
    }
}
