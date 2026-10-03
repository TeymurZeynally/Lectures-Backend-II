using Lecture09.Background.Quartz.VideoService.BackgroundServices.UploadsSimulator;
using Lecture09.Background.Quartz.VideoService.BackgroundServices.VideoProcessing.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Calendar;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddQuartz(q =>
{
    q.SchedulerName = "video-service";
    q.SchedulerId = $"node-{Environment.ProcessId}";
    q.MisfireThreshold = TimeSpan.FromSeconds(5);

    q.UsePersistentStore(store =>
    {
        store.UsePostgres(builder.Configuration.GetConnectionString("Postgres")!);
        store.UseSystemTextJsonSerializer();
        store.UseProperties = true;
        store.UseClustering(cluster =>
        {
            cluster.CheckinInterval = TimeSpan.FromSeconds(2);
            cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(5);
        });
    });

    // Пример 1. Обычная периодическая задача: запускается сразу после старта и дальше через равные промежутки.
    // ScheduleJob создаёт задачу и её триггер одним вызовом
    q.ScheduleJob<StuckVideosCheckJob>(
        trigger => trigger
            .WithIdentity("stuck-videos-check-every-1s")
            .StartNow()
            .WithSimpleSchedule(s => s.WithInterval(TimeSpan.FromMilliseconds(1)).RepeatForever()),
        job => job.WithIdentity(StuckVideosCheckJob.Key));

    // Пример 2. Одна задача очистки и два расписания к ней. Cron в Quartz начинается с секунд:
    // "сек мин час день месяц день_недели", поэтому "0/5 * * * * ?" - каждые 5 секунд начиная с :00.
    // Черновики чистим в :00, :05, :10..., временные файлы - в :02, :07, :12...
    // Каждый триггер передаёт задаче своё "что чистить".
    // Задача регистрируется отдельно от триггеров, поэтому нужен StoreDurably: Quartz удаляет задачу,
    // у которой нет ни одного триггера, а durable-задача хранится и без них.
    // В примере 1 это не нужно: ScheduleJob сразу создаёт задачу вместе с триггером
    q.AddJob<CleanupJob>(CleanupJob.Key, job => job.StoreDurably());

    // Если все узлы лежали и время запуска прошло - выполнить один раз сразу после подъёма
    q.AddTrigger(trigger => trigger
        .ForJob(CleanupJob.Key)
        .WithIdentity("drafts-cleanup")
        .UsingJobData("Target", "unpublished drafts")
        .WithCronSchedule("0/5 * * * * ?", cron => cron.WithMisfireHandlingInstructionFireAndProceed()));

    // А здесь пропущенные запуски просто пропускаем и ждём следующего по расписанию
    q.AddTrigger(trigger => trigger
        .ForJob(CleanupJob.Key)
        .WithIdentity("temp-files-cleanup")
        .UsingJobData("Target", "temporary transcoding files")
        .WithCronSchedule("2/5 * * * * ?", cron => cron.WithMisfireHandlingInstructionDoNothing()));

    // Пример 3. Расписание с исключениями. Календарь - это список времени, когда запускать НЕЛЬЗЯ.
    // "quiet-time" ("тихое время") - просто его имя. Здесь он запрещает каждые вторые 10 секунд минуты,
    // поэтому отчёт каждые 2 секунды идёт 10 секунд, потом 10 секунд пауза, и так по кругу.
    // В жизни так исключают праздники и выходные
    q.AddCalendar("quiet-time", new CronCalendar("10-19,30-39,50-59 * * * * ?"), replace: true, updateTriggers: true);
    q.ScheduleJob<AuthorsReportJob>(
        trigger => trigger
            .WithIdentity("authors-report-every-2s")
            .WithCronSchedule("0/2 * * * * ?")
            .ModifiedByCalendar("quiet-time"),
        job => job.WithIdentity(AuthorsReportJob.Key));

    // Пример 4. Однократная отложенная задача: публикация ролика через 10 секунд после загрузки.
    // При старте у неё нет триггеров - их добавляет UploadsSimulator, по одному на каждый ролик.
    // StoreDurably - чтобы задача не удалилась, пока триггеров нет, и после того, как разовый триггер отработал.
    // RequestRecovery - если узел упал посреди публикации, другой узел выполнит её заново
    q.AddJob<PublishVideoJob>(PublishVideoJob.Key, job => job.StoreDurably().RequestRecovery());
});

builder.Services.Configure<QuartzOptions>(options =>
{
    // Все узлы при старте регистрируют одни и те же задачи. Первый создаёт их в базе, остальные не перезаписывают,
    // иначе каждый перезапуск узла сбрасывал бы триггеры и время их следующего запуска
    options.Scheduling.IgnoreDuplicates = true;
    options.Scheduling.OverWriteExistingData = false;
});

builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// builder.Services.AddHostedService<UploadsSimulator>();

var host = builder.Build();

if(args.FirstOrDefault() == "cleanup")
{
    await(await host.Services.GetRequiredService<ISchedulerFactory>().GetScheduler()).Clear();
    return;
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();

using (logger.BeginScope("{Context}", nameof(Program)))
{
    logger.LogInformation("Node node-{Pid} started", Environment.ProcessId);
}

host.Run();
