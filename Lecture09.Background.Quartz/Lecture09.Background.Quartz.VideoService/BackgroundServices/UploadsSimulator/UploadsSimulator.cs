using Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lecture09.Background.Quartz.VideoService.BackgroundServices.UploadsSimulator;

public sealed class UploadsSimulator(
    ISchedulerFactory schedulerFactory,
    ILogger<UploadsSimulator> logger) : BackgroundService
{
    private static readonly string[] Titles =
    [
        "Путешествие на Байкал",
        "Обзор нового ноутбука",
        "Рецепт домашнего хлеба",
        "Сборка компьютера с нуля",
        "Уроки гитары для начинающих",
        "Разбор шахматной партии",
        "Ремонт велосипеда своими руками",
        "Таймлапс заката над морем",
        "Поход в горы: день первый",
        "Обзор настольной игры",
        "Студент объясняет пропущенный дедлайн 10 часов",
        "Утренняя пробежка в парке"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = logger.BeginScope("{Context}", nameof(UploadsSimulator));

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var videoId = Guid.NewGuid().ToString()[..8];
            var title = Titles[Random.Shared.Next(Titles.Length)];

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"publish-{videoId}", "publications")
                .ForJob(PublishVideoJob.Key)
                .UsingJobData("VideoId", videoId)
                .UsingJobData("Title", title)
                .StartAt(DateTimeOffset.UtcNow.AddSeconds(10))
                .WithSimpleSchedule(s => s.WithMisfireHandlingInstructionFireNow())
                .Build();

            var publishAt = await scheduler.ScheduleJob(trigger, stoppingToken);

            logger.LogInformation(
                "Video {VideoId} \"{Title}\" uploaded, publication scheduled at {PublishAt:HH:mm:ss}",
                videoId, title, publishAt.ToLocalTime());
        }
    }
}
