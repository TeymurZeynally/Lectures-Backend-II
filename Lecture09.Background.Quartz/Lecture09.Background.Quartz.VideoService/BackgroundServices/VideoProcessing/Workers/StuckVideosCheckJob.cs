using Microsoft.Extensions.Logging;
using Quartz;

namespace Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;

[DisallowConcurrentExecution]
public sealed class StuckVideosCheckJob(ILogger<StuckVideosCheckJob> logger) : IJob
{
    public static readonly JobKey Key = new("stuck-videos-check");

    public Task Execute(IJobExecutionContext context)
    {
        using var _ = logger.BeginScope("{Context}", nameof(StuckVideosCheckJob));

        logger.LogInformation(
            "Checking videos stuck in processing for more than an hour (scheduled at {Scheduled:HH:mm:ss})",
            context.ScheduledFireTimeUtc?.ToLocalTime());

        return Task.CompletedTask;
    }
}
