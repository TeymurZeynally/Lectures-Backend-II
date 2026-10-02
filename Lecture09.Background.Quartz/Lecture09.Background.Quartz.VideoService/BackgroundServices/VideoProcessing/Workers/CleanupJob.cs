using Microsoft.Extensions.Logging;
using Quartz;

namespace Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class CleanupJob(ILogger<CleanupJob> logger) : IJob
{
    public static readonly JobKey Key = new("cleanup");

    public Task Execute(IJobExecutionContext context)
    {
        using var _ = logger.BeginScope("{Context}", nameof(CleanupJob));

        var target = context.MergedJobDataMap.GetString("Target");
        var trigger = context.Trigger.Key.Name;
        var scheduledAt = context.ScheduledFireTimeUtc?.ToLocalTime();
        var firedAt = context.FireTimeUtc.ToLocalTime();

        logger.LogInformation(
            "Cleanup: deleting {Target} (trigger {Trigger}, scheduled at {Scheduled:HH:mm:ss}, fired at {Fired:HH:mm:ss})",
            target, trigger, scheduledAt, firedAt);

        return Task.CompletedTask;
    }
}
