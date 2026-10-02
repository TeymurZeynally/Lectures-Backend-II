using Microsoft.Extensions.Logging;
using Quartz;

namespace Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class PublishVideoJob(ILogger<PublishVideoJob> logger) : IJob
{
    public static readonly JobKey Key = new("publish-video");

    public async Task Execute(IJobExecutionContext context)
    {
        using var _ = logger.BeginScope("{Context}", nameof(PublishVideoJob));

        var videoId = context.MergedJobDataMap.GetString("VideoId");
        var title = context.MergedJobDataMap.GetString("Title");

        logger.LogInformation(
            "Publishing video {VideoId} \"{Title}\" (scheduled at {Scheduled:HH:mm:ss}{Recovering})",
            videoId, title, context.ScheduledFireTimeUtc?.ToLocalTime(),
            context.Recovering ? ", RECOVERING after a node failure" : "");
    }
}
