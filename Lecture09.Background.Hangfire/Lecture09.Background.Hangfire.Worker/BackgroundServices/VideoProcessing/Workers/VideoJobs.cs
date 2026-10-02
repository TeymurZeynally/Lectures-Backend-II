using Lecture09.Background.Hangfire.Contracts;
using Microsoft.Extensions.Logging;

namespace Lecture09.Background.Hangfire.Worker.BackgroundServices.VideoProcessing.Workers;

// Реализация задач. Сам Worker эти методы не вызывает - их вызывает Hangfire-сервер, когда достаёт задачу
// из очереди: создаёт VideoJobs через DI и вызывает метод с аргументами, сохранёнными при постановке задачи.
// Метод вернулся без исключения - задача Succeeded. Бросил исключение - задача повторяется по политике AutomaticRetry.
public sealed class VideoJobs(ILogger<VideoJobs> logger) : IVideoJobs
{
    // Разовая задача: ставится сразу при загрузке ролика
    public async Task ValidateAsync(string videoId, string title, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validating video {VideoId} \"{Title}\"", videoId, title);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        logger.LogInformation("Video {VideoId} is valid", videoId);
    }

    // Продолжение: попадает в очередь "transcoding" только после успешного завершения предыдущего этапа
    public async Task TranscodeAsync(string videoId, string quality, CancellationToken cancellationToken)
    {
        logger.LogInformation("Transcoding video {VideoId} to {Quality}", videoId, quality);
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        if (Random.Shared.NextDouble() < 0.2)
        {
            logger.LogWarning("Transcoding video {VideoId} to {Quality} failed", videoId, quality);

            // Исключение - сигнал Hangfire, что задача не выполнена. Фильтр [AutomaticRetry(Attempts = 3)]
            // на методе интерфейса поставит её на повтор, а Hangfire напишет в лог "Retry attempt N of 3"
            throw new InvalidOperationException($"Transcoder crashed on {quality}");
        }

        logger.LogInformation("Video {VideoId} transcoded to {Quality}", videoId, quality);
    }

    // Последнее продолжение цепочки: выполняется, только когда все три качества готовы
    public async Task PublishAsync(string videoId, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        logger.LogInformation("Video {VideoId} is published and available to viewers", videoId);
    }

    // Отложенная задача: лежит в Scheduled и попадает в очередь через минуту после загрузки
    public async Task DeleteOriginalAsync(string videoId, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        logger.LogInformation("Original file of video {VideoId} deleted", videoId);
    }

    // Периодическая задача: каждые 10 секунд её ставит в очередь один из Worker'ов, а выполняет любой
    public async Task RecalculateStatisticsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        logger.LogInformation("Views statistics recalculated");
    }
}
