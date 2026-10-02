using Hangfire;
using Hangfire.PostgreSql;
using Lecture09.Background.Hangfire.Contracts;
using Lecture09.Background.Hangfire.Worker.BackgroundServices.VideoProcessing.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

// Реализация контракта. Hangfire создаёт её через DI для каждого выполнения задачи
builder.Services.AddTransient<IVideoJobs, VideoJobs>();

builder.Services.AddHangfire(config => config
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        storage => storage.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres")),
        new PostgreSqlStorageOptions
        {
            // Как часто проверять очередь на новые задачи. По умолчанию 15 секунд - для демонстрации слишком долго
            QueuePollInterval = TimeSpan.FromSeconds(1),
            // Пока Worker жив, он продлевает захват своих задач. Если Worker упал, через InvisibilityTimeout
            // его задача снова становится видна в очереди, и её забирает другой Worker
            UseSlidingInvisibilityTimeout = true,
            InvisibilityTimeout = TimeSpan.FromSeconds(30)
        }));

// AddHangfireServer - то, что делает этот процесс сервером: запускает обработчики и фоновые процессы Hangfire
builder.Services.AddHangfireServer(options =>
{
    // Имя сервера в Dashboard (вкладка Servers). Hangfire сам допишет к нему уникальный суффикс
    options.ServerName = $"worker-{Environment.ProcessId}";
    // Какие очереди обслуживает этот Worker, в порядке приоритета
    options.Queues = ["transcoding", "default"];
    // Сколько задач выполняется параллельно (число потоков-обработчиков)
    options.WorkerCount = 2;
    // Как часто проверять отложенные задачи, повторы и периодические задачи. По умолчанию 15 секунд
    options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
});

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>().LogInformation(
    "Worker worker-{Pid} started", Environment.ProcessId);

host.Run();
