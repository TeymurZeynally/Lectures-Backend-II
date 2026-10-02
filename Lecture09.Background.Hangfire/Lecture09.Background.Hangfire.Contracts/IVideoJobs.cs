using Hangfire;

namespace Lecture09.Background.Hangfire.Contracts;

public interface IVideoJobs
{
    // Без атрибутов: очередь "default" и политика повторов по умолчанию - 10 попыток с растущей задержкой
    Task ValidateAsync(string videoId, string title, CancellationToken cancellationToken);

    // [Queue] - задача попадает в отдельную очередь "transcoding". Worker'ы сами выбирают, какие очереди обслуживать,
    // поэтому под перекодирование можно запустить отдельные экземпляры с большим числом потоков.
    // [AutomaticRetry] - своя политика повторов: если метод бросил исключение, задача ставится повторно
    // через 5, 10 и 20 секунд; после третьей неудачи задача переходит в состояние Failed и ждёт ручного перезапуска
    [Queue("transcoding")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [5, 10, 20])]
    Task TranscodeAsync(string videoId, string quality, CancellationToken cancellationToken);

    Task PublishAsync(string videoId, CancellationToken cancellationToken);

    Task DeleteOriginalAsync(string videoId, CancellationToken cancellationToken);

    Task RecalculateStatisticsAsync(CancellationToken cancellationToken);
}
