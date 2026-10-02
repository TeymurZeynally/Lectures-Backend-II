# Лекция 09 — Temporal

Обработка видеоролика, записанная обычным кодом на C# — как workflow
Temporal `ProcessVideo` со схемы на слайде "Основные понятия". **Worker** исполняет код
workflow и activities; его можно запускать в нескольких консолях и
останавливать в любой момент. **UploadService** — та же страница, что в
примерах с Hangfire и Airflow: загружает ролики (запускает workflow),
показывает их статус и даёт модератору кнопки Approve / Reject. **Temporal Server** развёрнут в Docker
теми же сервисами, что на схеме архитектуры: Frontend, History, Matching и
Worker service — отдельными контейнерами, Persistence — Postgres.

## Workflow

```
           ┌─► Transcode 1080p ─┐
           ├─► Transcode 720p  ─┤      ждать, что наступит раньше:
Validate ──┼─► Transcode 480p  ─┼─►    сигнал «approve»            ─► Publish ─► Notify
           └─► Preview ─────────┘      или таймер (1 минута)
           ▲                           сигнал «reject»
           └─────── на доработку ◄─────────┘
```

- **Validate** — проверка ролика.
- **Transcode** × 3 и **Preview** — выполняются параллельно, возможно, на
  разных Worker'ах. Workflow ждёт все четыре.
- **Ожидание решения модератора** — сигнал `approve` или таймер, что
  наступит раньше. В жизни такой таймер ставят на часы или дни (на слайде —
  24 часа), в примере — 1 минута, чтобы не ждать долго. Если сигнала не было, ролик публикуется автоматически.
- **Возврат на доработку** — сигнал `reject` с причиной отправляет ролик на
  новый круг перекодирования (`transcoding, round 2`), и он снова ждёт
  решения. В графе без циклов (DAG, как в Airflow) вернуться к пройденной
  вершине нельзя, а здесь это обычный `for`.
- **Publish** и **Notify** — публикация и уведомление подписчиков.

Код — в `BackgroundServices/VideoProcessing`: `Workflows/ProcessVideoWorkflow.cs`
(порядок этапов) и `Activities/VideoActivities.cs` (сами этапы). Полезная
нагрузка — сообщения в логе и `Task.Delay`. Перекодирование длится 6 секунд и
с вероятностью 30% падает.

## Концепции

- **Workflow — обычный код** — порядок этапов записан как `async`-метод:
  `await` — последовательность, `Workflow.WhenAllAsync` — параллельные ветки,
  а при необходимости — обычные `for`, `if` и `try`. Код workflow должен быть
  детерминированным: никакого `DateTime.Now`, `Random`, сети и файлов — всё
  это делают activities.
- **Activity** — обычный метод с любыми побочными эффектами (`[Activity]`).
  Workflow не вызывает его напрямую: `Workflow.ExecuteActivityAsync` просит
  сервер поставить задачу в очередь, а выполнит её любой Worker.
- **История событий** — сервер записывает каждый шаг: workflow запущен,
  activity поставлена, выполнена с таким-то результатом, таймер запущен,
  пришёл сигнал. Это и есть состояние workflow.
- **Восстановление после сбоя (replay)** — если Worker, на котором шёл
  workflow, упал, другой Worker получает историю и выполняет код workflow с
  начала. Activities, которые уже выполнены, повторно не запускаются: их
  результаты берутся из истории, и код доходит до того места, где
  остановился.
- **Повторы activities** — `RetryPolicy`: при исключении activity
  повторяется через 1, 2, 4, 8 секунд, всего до 5 попыток. Номер попытки —
  в логе (`attempt 2`).
- **Heartbeat** — перекодирование раз в секунду сообщает серверу, что оно
  живо. Если Worker упал, сервер замечает это через 3 секунды
  (`HeartbeatTimeout`) и отдаёт activity другому Worker'у. Activities без
  heartbeat будут повторены только по `StartToCloseTimeout` (30 секунд).
- **Таймеры и долгие ожидания** — `Workflow.WaitConditionAsync(..., 1 минута)`
  ждёт сигнал или таймер и не держит поток Worker'а: таймер хранится на
  сервере. Можно остановить все Worker'ы, и ожидание продолжится. Так же
  можно ждать и 24 часа, и 30 дней.
- **Сигналы и запросы** — `[WorkflowSignal("approve")]` передаёт данные в
  выполняющийся workflow (решение модератора), `[WorkflowQuery]` читает его
  состояние (`Status`) без изменения истории.
- **Очередь задач** — Worker'ы и UploadService договариваются только об
  имени очереди `video-processing` и имени workflow `ProcessVideo`.
  UploadService не ссылается на код workflow: он запускает его по имени.
  Id workflow — `video-<id ролика>`, второй workflow с тем же id не
  запустится.

## 1. Запуск Temporal

```bash
docker compose up -d
```

Поднимаются Postgres, четыре сервиса сервера (`temporal-frontend` на
`localhost:7233`, `temporal-history`, `temporal-matching`,
`temporal-worker`), веб-интерфейс и `temporal-init`. Init создаёт в
Postgres базы со схемами, ждёт, пока поднимется сервер, создаёт namespace
`default` и завершается — как `airflow-init` в примере с Airflow. Серверные
сервисы стартуют, как только схемы готовы. Worker'ы запускайте, когда init
перейдёт в `Exited (0)`:

```bash
docker compose ps -a
```

- UI: [http://localhost:8233](http://localhost:8233) — список workflow,
  история событий каждого, ожидающие activities с номером попытки.
- CLI — запускается одноразовым контейнером в сети проекта, ставить его
  на машину не нужно. Удобно завести alias:

  ```bash
  alias temporal='docker run --rm --network lecture09backgroundtemporal_default -e TEMPORAL_ADDRESS=temporal-frontend:7233 temporalio/admin-tools:1.31.3 temporal'
  ```
- Persistence — Postgres на `localhost:5434` (логин `temporal` / `temporal`,
  порт не 5432, чтобы не конфликтовать с другими примерами). Две базы:
  `temporal` — история событий, таймеры, очереди задач; `temporal_visibility`
  — список workflow для поиска в UI:

  ```sql
  -- в базе temporal_visibility
  select workflow_id, workflow_type_name, status, start_time, close_time
  from executions_visibility order by start_time desc limit 10;
  ```

  `status`: 1 — выполняется, 2 — завершён, 3 — упал, 4 — отменён,
  5 — принудительно остановлен, 7 — истёк таймаут.

## 2. Запуск Worker'ов

Каждый Worker в своей консоли:

```bash
cd Lecture09.Background.Temporal.Worker
dotnet run
```

При старте Worker печатает свой `pid`.

## 3. Запуск UploadService

```bash
cd Lecture09.Background.Temporal.UploadService
dotnet run
```

Страница: [http://localhost:5082](http://localhost:5082).

- "Загрузить" / "Загрузить 10" — запускают workflow `ProcessVideo` с id
  `video-<id ролика>` (`StartWorkflowAsync` по имени типа).
- У каждого ролика раз в полторы секунды обновляется статус. Пока workflow
  работает, статус читается запросом (Query) `Status` — на него отвечает
  Worker. Если Worker'ов нет, статус — `no worker to answer the query`.
  Завершённые workflow сервер описывает сам (`DescribeAsync`), Worker для
  этого не нужен.
- В статусе `waiting for approval` появляются кнопки Approve и Reject (с
  причиной) — они отправляют сигналы `approve` и `reject`.
- Ссылка `workflow video-...` открывает историю событий в Temporal UI.

## Остановка

```bash
docker compose down
```
