// Как работает Hangfire
//
// Hangfire - библиотека фоновой обработки для .NET: очередь задач, планировщик и панель мониторинга в одном пакете.
// Брокера сообщений нет: задачи, очереди, расписания и состояние серверов хранятся в обычной базе данных
// (здесь Postgres, схема "hangfire", таблицы job, jobqueue, state, set, hash, server, lock, ...).
//
// Задача (job) - это вызов обычного метода. Когда мы пишем
//     jobs.Enqueue<IVideoJobs>(x => x.ValidateAsync(videoId, title, CancellationToken.None));
// метод НЕ вызывается. Hangfire разбирает лямбду (Expression) и сохраняет в базу:
//     hangfire.job      - тип IVideoJobs, имя метода ValidateAsync и аргументы в JSON, состояние Enqueued;
//     hangfire.jobqueue - строку "задача N ждёт в очереди default".
// Дальше её заберёт и выполнит любой запущенный Hangfire-сервер.
//
// Участники:
// - Клиент - тот, кто ставит задачи (IBackgroundJobClient, IRecurringJobManager). Ему нужны только база
//   и интерфейс задачи, реализация не нужна. Здесь это UploadService.
// - Сервер - процесс, в котором вызван AddHangfireServer. В нём работают несколько потоков-обработчиков:
//   они опрашивают jobqueue, забирают задачу, десериализуют вызов, создают реализацию IVideoJobs из DI
//   и вызывают метод. Результат (Succeeded или Failed с исключением) записывается в базу.
//   Здесь это Worker, их можно запустить и остановить сколько угодно и когда угодно.
// - Панель (Dashboard) - веб-страница, которая читает те же таблицы: задачи по состояниям, очереди,
//   серверы, повторы, периодические задачи; позволяет перезапустить или удалить задачу.
//
// Состояния задачи: Enqueued (ждёт в очереди) -> Processing (выполняется) -> Succeeded / Failed.
// Кроме того: Scheduled (отложена до времени), Awaiting (ждёт завершения родительской задачи), Deleted.
//
// Проекты примера:
// - Contracts     - интерфейс IVideoJobs, общий контракт клиента и сервера.
// - UploadService - этот проект: страница с кнопками, контроллеры, которые ставят задачи, и Dashboard.
//                   Сам задачи не выполняет - в нём нет AddHangfireServer.
// - Worker        - консольный Hangfire-сервер с реализацией IVideoJobs. Выполняет задачи.

using Hangfire;
using Hangfire.PostgreSql;
using Lecture09.Background.Hangfire.Contracts;
using Lecture09.Background.Hangfire.UploadService.Api.Videos.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(config => config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddControllers();
builder.Services.AddScoped<IVideosService, VideosService>();

builder.Services.AddHangfire(config => config
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"))));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();


app.Services.GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate<IVideoJobs>(
        "recalculate-statistics",
        x => x.RecalculateStatisticsAsync(CancellationToken.None),
        "*/10 * * * * *");

app.Run();
