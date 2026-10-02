using Lecture09.Background.Temporal.Worker.BackgroundServices.VideoProcessing.Activities;
using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Lecture09.Background.Temporal.Worker.BackgroundServices.VideoProcessing.Workflows;

// Workflow - процесс обработки одного ролика, записанный обычным кодом (схема на слайде "Основные понятия"):
// Validate -> параллельно Transcode 1080p / 720p / 480p и Preview -> ждать approve или таймер -> Publish -> Notify.

// На каждый запуск (каждый ролик) создаётся свой экземпляр этого класса со своими полями.
// Состояние экземпляра (поля, место в коде) не хранится в памяти "навсегда": сервер хранит историю событий,
// и любой Worker может восстановить экземпляр, выполнив RunAsync заново по этой истории (replay).

// Поэтому код workflow обязан быть детерминированным: при повторном выполнении по той же истории
// он должен принимать те же решения. Нельзя: DateTime.Now, Random, Guid.NewGuid, Task.Delay, Task.Run,
// HTTP, файлы, база. Всё это делают activities, а здесь есть безопасные замены: Workflow.UtcNow,
// Workflow.Random, Workflow.DelayAsync, Workflow.WhenAllAsync, Workflow.WaitConditionAsync.
[Workflow("ProcessVideo")]
public class ProcessVideoWorkflow
{
    // Решение модератора: выставляется сигналами approve / reject, читается в RunAsync
    private string? _decision;
    private string? _rejectReason;

    [WorkflowQuery] // запрос: внешний код может прочитать это значение у работающего workflow, в историю не пишется
    public string Status { get; private set; } = "started";

    // Сигнал: внешний код передаёт данные в работающий workflow, здесь - решение модератора.
    // Сигнал записывается в историю, поэтому при replay обработчик вызовется снова в том же месте.
    // Обработчик должен быть коротким: он только меняет состояние, а реагирует на него код в RunAsync
    [WorkflowSignal("approve")]
    public Task ApproveAsync()
    {
        _decision = "approved";
        return Task.CompletedTask;
    }

    // Сигнал с параметром: temporal workflow signal ... --name reject --input '"причина"'
    [WorkflowSignal("reject")]
    public Task RejectAsync(string reason)
    {
        _decision = "rejected";
        _rejectReason = reason;
        return Task.CompletedTask;
    }

    // Точка входа. Аргументы приходят из команды запуска (videoId, title от UploadService).
    // Когда метод вернулся - workflow завершён успешно, бросил исключение - завершён с ошибкой
    [WorkflowRun]
    public async Task RunAsync(string videoId, string title)
    {
        // Настройки выполнения activity:
        // StartToCloseTimeout - сколько может длиться одна попытка. Не уложилась (или Worker с ней пропал) -
        // попытка считается неудачной и повторяется. Повторы - по политике по умолчанию:
        // неограниченно, через 1, 2, 4, 8 ... секунд, но не реже раза в 100 секунд
        var options = new ActivityOptions { StartToCloseTimeout = TimeSpan.FromSeconds(30) };

        var transcodeOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(30),
            HeartbeatTimeout = TimeSpan.FromSeconds(3),
            RetryPolicy = new RetryPolicy { InitialInterval = TimeSpan.FromSeconds(1), BackoffCoefficient = 2, MaximumAttempts = 5 }
        };

        Status = "validating";

        // ExecuteActivityAsync не вызывает метод здесь. Лямбда только говорит, какую activity и с какими аргументами
        // запустить: сервер записывает в историю "activity запланирована" и кладёт задачу в очередь,
        // её выполняет любой Worker. await вернётся, когда в истории появится "activity завершена".
        // При replay на другом Worker'е activity уже не выполняется - результат берётся из истории
        await Workflow.ExecuteActivityAsync((VideoActivities a) => a.ValidateAsync(videoId, title), options);

        string[] renditions;
        string preview;

        // Бесконечный цикл "перекодирование -> решение модератора": пока ролик отклоняют, он идёт на новый круг.
        // Выходим из цикла через break, когда ролик одобрили или решения не было
        for (var round = 1; ; round++)
        {
            Status = $"transcoding, round {round}";

            // Workflow.Logger не пишет в лог во время replay, иначе при восстановлении на другом Worker'е все старые сообщения вывелись бы повторно
            Workflow.Logger.LogInformation("Video {VideoId}: transcoding, round {Round}", videoId, round);

            // Параллельно: три качества и превью. Каждый вызов ExecuteActivityAsync сразу возвращает задачу (Task),
            var transcodeTasks = new[] { "1080p", "720p", "480p" }
                .Select(quality => Workflow.ExecuteActivityAsync((VideoActivities a) => a.TranscodeAsync(videoId, quality), transcodeOptions))
                .ToArray();
            var previewTask = Workflow.ExecuteActivityAsync((VideoActivities a) => a.PreviewAsync(videoId), options);

            // Точка сбора: ждём все четыре. Workflow.WhenAllAsync - безопасная для workflow форма Task.WhenAll:
            renditions = await Workflow.WhenAllAsync(transcodeTasks);
            preview = await previewTask;

            Status = "waiting for approval";
            Workflow.Logger.LogInformation("Video {VideoId}: waiting for approval (1 minute)", videoId);

            _decision = null;

            // "Ждать то, что наступит раньше": сигнал (approve или reject) или таймер в 1 минуту.
            // Возвращает true, если условие выполнилось (пришёл сигнал), и false, если сработал таймер
            if (!await Workflow.WaitConditionAsync(() => _decision != null, TimeSpan.FromMinutes(1)))
            {
                Workflow.Logger.LogInformation("Video {VideoId}: no decision in time, published automatically", videoId);
                break;
            }

            if (_decision == "approved")
            {
                Workflow.Logger.LogInformation("Video {VideoId}: approved by moderator", videoId);
                break;
            }

            Workflow.Logger.LogInformation("Video {VideoId}: rejected by moderator ({Reason}), back to transcoding", videoId, _rejectReason);
        }

        Status = "publishing";
        await Workflow.ExecuteActivityAsync((VideoActivities a) => a.PublishAsync(videoId, renditions, preview), options);
        await Workflow.ExecuteActivityAsync((VideoActivities a) => a.NotifyAsync(videoId), options);

        Status = "published";
        Workflow.Logger.LogInformation("Video {VideoId}: published", videoId);
    }
}
