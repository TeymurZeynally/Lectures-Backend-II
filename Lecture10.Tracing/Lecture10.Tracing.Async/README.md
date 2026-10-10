# Лекция 10 — Трассировка асинхронного взаимодействия

Сервис записи сохраняет запись, публикует событие `appointment.created` и
сразу отвечает клиенту. Событие независимо обрабатывают две группы
потребителей:

```
POST /appointments                         appointments-api  :5120
├─ INSERT appointments (clinic-db)
└─ send appointment.created ──> Kafka, топик appointment.created (3 партиции)
                                 ├─ группа notifications ─ process appointment.created ─ POST /sms (sms-gateway)
                                 └─ группа reminders     ─ process appointment.created ─ INSERT reminders (reminders-db)
```

| Проект | Сервис |
|---|---|
| `Lecture10.Tracing.Async.AppointmentsApi` | `appointments-api`, REST API на порту 5120, Swagger — http://localhost:5120/swagger |
| `Lecture10.Tracing.Async.Notifications` | `notifications`, консольное приложение, группа `notifications`: отправляет SMS о записи |
| `Lecture10.Tracing.Async.Reminders` | `reminders`, консольное приложение, группа `reminders`: сохраняет напоминание за день до приёма |
| `Lecture10.Tracing.Async.Contracts` | сообщение `AppointmentCreated` |

Kafka поднимается `docker-compose.yml` из этой папки:
брокер на `localhost:29092` и Kafka UI — http://localhost:8082 (Topics →
`appointment.created` → Messages → Headers: там виден `traceparent`).
SMS-шлюз не настоящий: отправка изображается задержкой 50–120 мс, в 3%
случаев — 1–1,5 с, в 5% случаев шлюз отвечает 503. База напоминаний тоже
изображается задержкой, в 2% случаев `INSERT reminders` завершается ошибкой.

Трейс одного запроса — три сервиса, ветки потребителей заканчиваются
позже ответа клиенту:

```
appointments-api  POST appointments                              SERVER
appointments-api    AppointmentsController.Create                INTERNAL
appointments-api      AppointmentsService.CreateAsync            INTERNAL
appointments-api        AppointmentsRepository.InsertAsync       INTERNAL
appointments-api          INSERT appointments                    CLIENT
appointments-api        AppointmentEventsProducer.PublishCreatedAsync INTERNAL
appointments-api          send appointment.created               PRODUCER
reminders                   process appointment.created          CONSUMER
reminders                     RemindersRepository.InsertAsync    INTERNAL
reminders                       INSERT reminders                 CLIENT
notifications               process appointment.created          CONSUMER
notifications                 SmsGatewayClient.SendAsync         INTERNAL
notifications                   POST /sms                        CLIENT
```

## Концепции

- **Передача контекста через Kafka** — у клиента Kafka (`Confluent.Kafka`)
  нет автоматической трассировки, поэтому контекст передаётся вручную:
  - отправитель (`AppointmentEventsProducer`) создаёт спан вида Producer
    `send appointment.created` и записывает его контекст в заголовки
    сообщения: `Propagators.DefaultTextMapPropagator.Inject(...)`;
  - получатель (`AppointmentCreatedConsumer`) читает контекст из заголовков
    (`Extract(...)`) и создаёт спан вида Consumer
    `process appointment.created`, указав этот контекст родителем.

  Атрибуты спанов — из семантических соглашений для сообщений:
  `messaging.system`, `messaging.destination.name`,
  `messaging.consumer.group.name`, `messaging.kafka.offset`.
- **Время в топике** — промежуток между окончанием спана `send` и началом
  спана `process`. Потребители пишут его и в лог: `waited in topic ... ms`.
- **Отставание группы** — `Reminders` обрабатывает сообщения по одному:
  читает следующее, только когда закончил предыдущее. Если потребитель не
  успевает за отправителем, сообщения копятся в топике, и ветка группы
  появляется в трейсе позже — когда очередь дойдёт до сообщения. Отставание
  видно в Kafka UI (Consumers → группа → Lag) и по растущему
  `waited in topic`; догнать можно, запустив ещё экземпляры группы — они
  поделят между собой 3 партиции. `Notifications` не ждёт отправки SMS:
  прочитал сообщение, запустил обработку и сразу читает следующее, поэтому
  даже медленный SMS-шлюз не задерживает остальные сообщения. Цена — смещение
  сохраняется при чтении, а не после отправки: если процесс упадёт, SMS из
  начатых обработок не уйдут, и порядок отправки не гарантирован.
- **Две группы — две ветки трейса** — каждая группа получает своё
  сообщение и добавляет в трейс свою ветку. Трейс продолжается после того,
  как клиент получил ответ: спаны потребителей заканчиваются позже
  корневого `POST appointments`.
- **Граф вызовов для Kafka** — Tempo строит ребро по паре Producer →
  Consumer и, найдя пару, перестаёт ждать вторую. Поэтому из двух групп на
  графе видна одна — та, чей спан пришёл первым (`reminders`, запись в
  базу быстрее отправки SMS). В самом трейсе есть обе ветки. Узел
  `sms-gateway` появился из атрибута `peer.service` клиентского спана
  `POST /sms`: так на графе показывают внешнюю систему без трассировки.
- **Ручные спаны** — в каждом слое: контроллер, сервис, репозитории,
  отправитель и клиент SMS-шлюза начинают свой спан
  (`AppTracing.Source.StartActivity("Класс.Метод")`). Спан
  `process appointment.created` — это обработка сообщения в потребителе.
- **Traces** → Service `appointments-api` → таблицы трейсов → клик по Trace ID →
  "Show trace on this dashboard": трейс со спанами всех сервисов;
- **Traces** → "Service graph" — граф вызовов;
- **Logs** → Service All — логи всех сервисов, в начале строки имя сервиса.

Сообщения и их заголовки — в Kafka UI: http://localhost:8082 → Topics →
`appointment.created` → Messages.

## Остановка

`Ctrl+C` в консолях сервисов и нагрузки, затем Kafka (из этой папки):

```bash
docker compose down
```

Общая инфраструктура останавливается из папки `Lecture10.Tracing`:
`docker compose down`.
