using Microsoft.Extensions.Logging;
using Temporalio.Activities;

namespace Lecture09.Background.Temporal.Worker.BackgroundServices.VideoProcessing.Activities;

// Activities - отдельные шаги обработки. Это обычные методы, в них можно всё, что нельзя в workflow:
// ходить в сеть, базу, файлы, использовать Random и текущее время.
//
// Workflow их не вызывает напрямую: он просит сервер запланировать activity, сервер кладёт задачу в очередь
// "video-processing", и её выполняет любой свободный Worker. Аргументы и результат передаются через сервер в JSON
// и сохраняются в истории workflow, поэтому передавать стоит небольшие данные: id, пути к файлам.
//
// Activity может выполниться больше одного раза: при ошибке, по таймауту или если Worker упал посреди работы.
// Поэтому activities должны быть идемпотентными - повторное выполнение не должно портить результат.
//
// Класс зарегистрирован как AddScopedActivities: на каждое выполнение создаётся новый объект,
// зависимости приходят через конструктор
public class VideoActivities(ILogger<VideoActivities> logger)
{
    [Activity] // метод становится activity. Имя activity - имя метода без Async: "Validate"
    public async Task ValidateAsync(string videoId, string title)
    {
        logger.LogInformation("Validating video {VideoId} \"{Title}\"", videoId, title);

        // ActivityExecutionContext.Current - контекст текущего выполнения: номер попытки, id workflow,
        // токен отмены и heartbeat. Токен отменяется, когда Worker останавливают или сервер отменил попытку
        await Task.Delay(TimeSpan.FromSeconds(2), ActivityExecutionContext.Current.CancellationToken);
    }

    [Activity]
    public async Task<string> TranscodeAsync(string videoId, string quality)
    {
        var context = ActivityExecutionContext.Current;

        // Info.Attempt - номер попытки: 1, а после ошибок по RetryPolicy - 2, 3 ...
        logger.LogInformation("Transcoding video {VideoId} to {Quality}, attempt {Attempt}", videoId, quality, context.Info.Attempt);

        for (var second = 0; second < 6; second++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);

            // Heartbeat - "я жив, работаю". Сервер ждёт его не реже HeartbeatTimeout (3 секунды в workflow),
            // поэтому падение Worker'а посреди долгой работы замечается за секунды, а не по StartToCloseTimeout
            context.Heartbeat();
        }

        if (Random.Shared.NextDouble() < 0.3)
        {
            logger.LogWarning("Transcoding video {VideoId} to {Quality} failed", videoId, quality);

            // Исключение - попытка неудачна. Сервер повторит activity по RetryPolicy (через 1, 2, 4, 8 секунд),
            // возможно, на другом Worker'е. Workflow в это время просто ждёт свой await
            throw new InvalidOperationException($"Transcoder crashed on {quality}");
        }

        logger.LogInformation("Video {VideoId} transcoded to {Quality}", videoId, quality);

        // Возвращаемое значение уходит на сервер, записывается в историю и становится результатом
        // await Workflow.ExecuteActivityAsync(...) в workflow
        return $"{videoId}/{quality}.mp4";
    }

    [Activity]
    public async Task<string> PreviewAsync(string videoId)
    {
        logger.LogInformation("Creating preview of video {VideoId}", videoId);
        await Task.Delay(TimeSpan.FromSeconds(3), ActivityExecutionContext.Current.CancellationToken);
        return $"{videoId}/preview.jpg";
    }

    [Activity]
    public async Task PublishAsync(string videoId, string[] renditions, string preview)
    {
        logger.LogInformation("Publishing video {VideoId}: {Renditions}, {Preview}", videoId, string.Join(", ", renditions), preview);
        await Task.Delay(TimeSpan.FromSeconds(1), ActivityExecutionContext.Current.CancellationToken);
    }

    [Activity]
    public async Task NotifyAsync(string videoId)
    {
        logger.LogInformation("Notifying subscribers about video {VideoId}", videoId);
        await Task.Delay(TimeSpan.FromSeconds(1), ActivityExecutionContext.Current.CancellationToken);
    }
}
