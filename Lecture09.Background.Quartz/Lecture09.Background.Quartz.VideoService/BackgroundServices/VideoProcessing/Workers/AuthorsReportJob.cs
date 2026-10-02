using Microsoft.Extensions.Logging;
using Quartz;

namespace Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;

public sealed class AuthorsReportJob(ILogger<AuthorsReportJob> logger) : IJob
{
    public static readonly JobKey Key = new("authors-report");

    public Task Execute(IJobExecutionContext context)
    {
        using var _ = logger.BeginScope("{Context}", nameof(AuthorsReportJob));

        logger.LogInformation(
            "Sending views report to authors (scheduled at {Scheduled:HH:mm:ss}, next at {Next:HH:mm:ss})",
            context.ScheduledFireTimeUtc?.ToLocalTime(), context.NextFireTimeUtc?.ToLocalTime());

        return Task.CompletedTask;
    }
}
