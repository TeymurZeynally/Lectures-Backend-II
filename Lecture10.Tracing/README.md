# Лекция 10 — Распределённая трассировка

Общая инфраструктура наблюдаемости и учебные сервисы ветеринарной клиники.
Каждый сервис через OpenTelemetry отправляет в **OpenTelemetry Collector**
все три вида телеметрии: логи, метрики и трейсы. Collector раскладывает их
по хранилищам, а смотреть всё вместе можно в **Grafana**.

```
                                    ┌─> Prometheus (метрики) ─┐
appointments-api ──OTLP/HTTP──> Collector ─> Loki (логи) ─────├─> Grafana
другие сервисы   ──OTLP/HTTP──┘     └─> Tempo (трейсы) ───────┘
                                            └─ метрики по спанам ─> Prometheus
```

Инфраструктура поднимается один раз и общая для всех примеров. Сервисы
запускаются через `dotnet run` в отдельных консолях.

| Компонент | Адрес | Что делает | Конфигурация |
|---|---|---|---|
| OpenTelemetry Collector | `localhost:4317` (gRPC), `localhost:4318` (HTTP) | принимает OTLP от сервисов, отправляет данные в хранилища | `infrastructure/otel-collector/config.yaml` |
| Prometheus | http://localhost:9090 | метрики, принимает их через remote write (`/api/v1/write`) | из образа |
| Loki | http://localhost:3100 | логи, принимает OTLP (`/otlp/v1/logs`) | из образа |
| Tempo | http://localhost:3200 | трейсы, принимает OTLP/gRPC от Collector | `infrastructure/tempo/tempo.yaml` |
| Grafana | http://localhost:3000 | графики, логи и трейсы, вход без логина | `infrastructure/grafana/` |

Grafana настраивается через provisioning:

- `infrastructure/grafana/provisioning/datasources` — источники данных Prometheus, Loki и Tempo;
- `infrastructure/grafana/provisioning/dashboards` — откуда загружать дашборды;
- `infrastructure/grafana/dashboards` — сами дашборды, в Grafana они в папке "Dashboards":
  - **Service metrics** — метрики .NET и ASP.NET Core: память, GC, JIT,
    потоки, исключения, Kestrel, HTTP-сервер, маршрутизация;
  - **Logs** — число записей по уровням и сами логи: сверху вниз по времени,
    в начале строки имя сервиса; фильтры вверху — Service (можно несколько
    или All), Severity (уровни) и Trace ID;
  - **Traces** — метрики по спанам, длительность запросов с exemplars, граф
    вызовов и списки трейсов: последние, медленные и с ошибками.

## Примеры

### Lecture10.Tracing.SimpleApp — простое приложение

Сервис записи на приём `appointments-api` с одним запросом — расписание
врача на день:

```
GET /appointments?doctorId=51d0f3c4-8a2e-4b7f-9c1d-3e5a7b9f2c60&date=2026-10-08
```

Запрос проходит контроллер, сервис и репозиторий:

```
AppointmentsController.GetByDoctor
└─ AppointmentsService.GetDoctorDayAsync
   ├─ AppointmentsService.CheckAccessAsync
   │  └─ AppointmentsRepository.GetDoctorAsync
   │     └─ SELECT doctors
   ├─ AppointmentsRepository.FindByDoctorAndDateAsync
   │  └─ SELECT appointments
   └─ AppointmentsService.MapToResponse
```

Настоящей базы данных нет: репозиторий изображает запросы к PostgreSQL
задержкой и создаёт для них такие же спаны, какие создаёт Npgsql. Обычно
`SELECT appointments` выполняется 20–60 мс, но в 5% запросов — 1,5–1,9 с, а в
2% запросов завершается ошибкой `TimeoutException`. Если врача нет в
списке, сервис отвечает 404.

Врачи (`doctorId`):

| id | Врач |
|---|---|
| `51d0f3c4-8a2e-4b7f-9c1d-3e5a7b9f2c60` | Соколова Анна Викторовна, терапевт |
| `7e2b9c41-5d3a-4f68-a1b2-c4d5e6f70812` | Морозов Илья Сергеевич, хирург |
| `a3c58e17-2b4d-4c9a-8f61-0d2e3b4c5a69` | Лебедева Ольга Павловна, дерматолог |
| `c9f14d26-7e8b-4a35-b0c2-6d1e9f8a7b43` | Кузнецов Дмитрий Андреевич, кардиолог |
| `e4a7b392-1c6f-4d8e-9a05-b3c2d1e0f987` | Новикова Мария Игоревна, офтальмолог |

### Lecture10.Tracing.Sync — синхронное взаимодействие

Запись на приём проходит через несколько сервисов, которые вызывают друг
друга по gRPC и HTTP: `appointments-api` → `schedule-service`,
`patients-service` и внешний платёжный шлюз без трассировки. Описание,
запуск и нагрузка — в [Lecture10.Tracing.Sync/README.md](Lecture10.Tracing.Sync/README.md).

### Lecture10.Tracing.Async — асинхронное взаимодействие через Kafka

Сервис записи публикует событие `appointment.created`, его независимо
обрабатывают две группы потребителей — `notifications` и `reminders`.
Описание, запуск Kafka, сервисов и нагрузки — в
[Lecture10.Tracing.Async/README.md](Lecture10.Tracing.Async/README.md).

## Концепции

- **Подключение OpenTelemetry** — одна секция `AddOpenTelemetry()` в
  `Program.cs`: имя сервиса (`service.name = appointments-api`), какие
  сигналы собирать (`WithLogging`, `WithTracing`, `WithMetrics`) и куда
  отправлять — `UseOtlpExporter()` для всех трёх сразу.
- **Адрес Collector** — стандартные переменные окружения OpenTelemetry,
  заданы в `Properties/launchSettings.json`: `OTEL_EXPORTER_OTLP_ENDPOINT` —
  адрес, `OTEL_EXPORTER_OTLP_PROTOCOL` — `http/protobuf`. SDK сам отправляет
  логи на `/v1/logs`, метрики — на `/v1/metrics`, трейсы — на `/v1/traces`.
- **Логи** — обычный `ILogger`, записи обрабатывает Serilog: он выводит их
  в консоль и с `writeToProviders: true` передаёт другим провайдерам
  логирования. Один из них добавляет `WithLogging()` — он отправляет записи
  в Collector. `builder.Logging.ClearProviders()` убирает стандартный
  консольный провайдер .NET, иначе каждая запись печаталась бы дважды.
- **Структурированная запись** — в Loki у записи есть текст сообщения
  (`IncludeFormattedMessage` в `appsettings.json`), шаблон
  (`_OriginalFormat_`) и значения отдельными полями (`DoctorId`,
  `ElapsedMs`, `Rows`). К каждой записи SDK добавляет `trace_id` и `span_id`
  текущего спана. Логи пишут все три слоя: контроллер (запрос и ответ),
  сервис (проверка доступа, сборка ответа) и репозиторий (время запроса к
  базе, медленный запрос, ошибка).
- **Автоматическое инструментирование** — `AddAspNetCoreInstrumentation()`
  создаёт корневой спан входящего запроса (`GET appointments`, вид Server) и
  метрики `http.server.*`, `kestrel.*`, `aspnetcore.*`.
  `AddRuntimeInstrumentation()` добавляет метрики `dotnet.*` (GC, JIT, потоки).
- **Ручные спаны** — `ActivitySource` из `Tracing/AppTracing.cs`. В .NET спан
  называется `Activity`: `StartActivity("AppointmentsService.GetDoctorDayAsync")`
  начинает спан, а `using` завершает его в конце метода. Родителем
  становится спан, который был текущим в момент старта, поэтому вложенные
  вызовы методов превращаются во вложенные спаны.
- **Спаны базы данных** — вид `Client` и атрибуты из семантических
  соглашений: `db.system.name`, `db.namespace`, `db.query.text`,
  `db.response.returned_rows`. При ошибке спан получает статус `Error` и
  событие с исключением (`AddException`), поэтому видно, на каком шаге
  возникла ошибка.
- **Отправка раз в секунду** — по умолчанию SDK отправляет метрики раз в
  60 секунд, а накопленные спаны — раз в 5 секунд. В примерах в
  `launchSettings.json` оба интервала уменьшены до секунды
  (`OTEL_METRIC_EXPORT_INTERVAL=1000`, `OTEL_BSP_SCHEDULE_DELAY=1000`), чтобы
  данные появлялись в Grafana почти сразу. Tempo тоже считает метрики по
  спанам раз в секунду (`collection_interval` в `tempo.yaml`). У источника
  Prometheus в Grafana при этом `timeInterval: 5s`: от него зависит
  `$__rate_interval` — окно для `rate()` на графиках (не меньше 4
  интервалов, то есть 20 секунд), поэтому графики не скачут.
- **Exemplars** — `SetExemplarFilter(ExemplarFilterType.TraceBased)`: к
  измерениям гистограммы `http.server.request.duration` прикладывается
  `trace_id` запроса. Prometheus хранит их отдельно от временных рядов
  (`--enable-feature=exemplar-storage`), а на графике длительности они
  показаны точками, по клику открывается трейс медленного запроса.
- **Метрики из трейсов** — Collector отправляет спаны в Tempo, а
  компонент Tempo `metrics_generator` (`infrastructure/tempo/tempo.yaml`)
  на лету считает по ним метрики и раз в секунду отправляет их в
  Prometheus через remote write — тот же `/api/v1/write`, что и у Collector.
  Хранятся они в Prometheus, как обычные метрики:
  - `traces_spanmetrics_*` — число вызовов, ошибки и гистограмма
    длительности каждого спана (метки `service`, `span_name`,
    `span_kind`, `status_code`);
  - `traces_service_graph_*` — граф вызовов: Tempo сопоставляет пары
    спанов Client → Server и считает запросы между сервисами. На графе
    `user → appointments-api → clinic-db`: `clinic-db` — база данных из атрибута
    `db.namespace` спанов `SELECT`.
- **Метрики в Prometheus** — имя получает единицу и суффикс
  (`http.server.request.duration` → `http_server_request_duration_seconds_bucket`),
  атрибуты становятся метками, `service.name` — меткой `job`, а
  `service.instance.id` — меткой `instance`. У каждого запуска сервиса свой
  `instance`.
- **Переходы между источниками в Grafana** — настраиваются в
  `datasources.yml`: у Prometheus `exemplarTraceIdDestinations` (exemplar →
  трейс в Explore), у Loki `derivedFields` (поле `trace_id` записи лога →
  трейс в Explore), у Tempo `tracesToLogsV2` (спан → логи этого трейса в
  Loki), `tracesToMetrics` (спан → график p99 длительности этой операции в
  Prometheus) и `serviceMap` (откуда брать метрики для графа вызовов).
- **Спан → метрика** — `tracesToMetrics` в `datasources.yml` — это ссылка на
  запрос к Prometheus, в который Grafana подставляет данные спана, на котором
  нажали: `$__tags` — метки из его атрибутов по списку `tags`
  (`service.name` → `service="appointments-api"`), `${__span.name}` — имя
  операции. Получается p99 длительности именно этой операции по метрике
  Tempo `traces_spanmetrics_latency_bucket` за время трейса ± 2 минуты. В
  файле `$` записан как `$$`: одиночный `$` Grafana при загрузке
  provisioning считает переменной окружения. На открывшемся графике можно
  включить Exemplars (Options → Exemplars): точки — отдельные спаны этой
  операции, по клику — их трейсы. Tempo кладёт id трейса в exemplar под
  меткой `traceID`, а приложения — под `trace_id`, поэтому у источника
  Prometheus ссылка на трейс настроена для обеих.
  Переходы с дашборда на дашборд — обычные ссылки (data links) в панелях:
  они передают `trace_id` переменной в адресе, `?var-trace_id=...`. Всё, что
  ведёт на другую страницу, открывается в новой вкладке, а "Show trace on
  this dashboard" — в текущей.

## 1. Запуск инфраструктуры

```bash
docker compose up -d
```

## 2. Простое приложение

```bash
cd Lecture10.Tracing.SimpleApp
dotnet run
```

Сервис слушает http://localhost:5100, Swagger — http://localhost:5100/swagger.
Один запрос:

```bash
curl "http://localhost:5100/appointments?doctorId=51d0f3c4-8a2e-4b7f-9c1d-3e5a7b9f2c60&date=2026-10-08"
```

Нагрузка — примерно 12 запросов в секунду: каждые полсекунды по запросу к
каждому врачу и один к несуществующему (404). В консоли печатаются коды
ответов, остановить — `Ctrl+C`.

```bash
while true; do
  for doctor in 51d0f3c4-8a2e-4b7f-9c1d-3e5a7b9f2c60 7e2b9c41-5d3a-4f68-a1b2-c4d5e6f70812 \
      a3c58e17-2b4d-4c9a-8f61-0d2e3b4c5a69 c9f14d26-7e8b-4a35-b0c2-6d1e9f8a7b43 \
      e4a7b392-1c6f-4d8e-9a05-b3c2d1e0f987 00000000-0000-0000-0000-000000000000; do
    curl -s -o /dev/null -w "%{http_code} " \
      "http://localhost:5100/appointments?doctorId=$doctor&date=2026-10-08" &
  done
  sleep 0.5
done
```

## 3. Grafana

http://localhost:3000 → Dashboards → "Dashboards":

- **Traces** → таблицы "Recent traces", "Slow traces (> 1 s)", "Traces with
  errors" → клик по Trace ID:
  - "Show trace on this dashboard" — трейс появляется внизу дашборда
    диаграммой: дерево спанов с длительностями, видно, что время ушло на
    `SELECT appointments`. В трейсе с ошибкой у `SELECT appointments` статус
    Error и событие с исключением. Trace ID можно и вставить вручную в поле
    `Trace ID` вверху дашборда;
  - "Show logs of this trace (new tab)" — дашборд **Logs** в новой вкладке,
    отфильтрованный по этому трейсу. Та же ссылка есть в заголовке панели
    трейса ("Logs of this trace").
  - у каждого спана в трейсе — ссылки "Logs for this trace" (логи трейса от
    сервиса этого спана) и "Operation latency p99" (график p99 длительности
    этой операции), обе открывают Explore в новой вкладке.
- **Logs** → в деталях записи у поля `trace_id` ссылка "Open trace" —
  трейс в Explore (запрос к Tempo по `trace_id`) в новой вкладке. Поле
  `Trace ID` вверху дашборда Logs оставляет только логи одного трейса,
  пустое — все логи сервиса.
- **Traces** → "HTTP request duration (with exemplars)": точки exemplars, в
  подсказке точки ссылка "Open trace" — трейс этого запроса в Explore в
  новой вкладке.
- **Traces** → "Service graph" — граф вызовов всех запущенных сервисов,
  их баз данных и внешних систем.
- **Service metrics** и **Logs** — выбрать сервис в списке `Service` вверху,
  на Logs ещё уровни в `Severity`.
- **Explore** → Tempo → TraceQL, например:
  `{ resource.service.name = "appointments-api" && name = "SELECT appointments" && duration > 1s }`.
- **Explore** → Loki: `{service_name="appointments-api"} | detected_level="error"`,
  в деталях записи — её поля.

## Остановка

`Ctrl+C` в консолях сервисов и нагрузки, затем:

```bash
docker compose down
```

Удалить вместе с накопленными данными — `docker compose down -v`. Kafka
из асинхронного примера останавливается в его папке.
