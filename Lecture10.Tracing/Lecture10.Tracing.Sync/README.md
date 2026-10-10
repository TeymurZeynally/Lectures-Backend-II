# Лекция 10 — Трассировка синхронного взаимодействия

Запись на приём проходит через несколько сервисов, которые вызывают друг
друга по gRPC и HTTP:

```
POST /appointments                     appointments-api  :5110
├─ gRPC ReserveSlot     ──────────────> schedule-service  :5111 ─ UPDATE slots (schedule-db)
├─ gRPC GetMedicalCard  ──────────────> patients-service  :5112 ─ SELECT medical_cards (patients-db)
├─ HTTP POST /payments  ──────────────> payment-gateway   :5113 (внешний, без трассировки)
└─ INSERT appointments (clinic-db)
```

| Проект | Сервис | Порт |
|---|---|---|
| `Lecture10.Tracing.Sync.AppointmentsApi` | `appointments-api`, REST API, Swagger — http://localhost:5110/swagger | 5110 |
| `Lecture10.Tracing.Sync.ScheduleService` | `schedule-service`, gRPC `clinic.ScheduleService` | 5111 |
| `Lecture10.Tracing.Sync.PatientsService` | `patients-service`, gRPC `clinic.PatientsService` | 5112 |
| `Lecture10.Tracing.Sync.PaymentGateway` | платёжный шлюз — внешняя система, OpenTelemetry не подключён | 5113 |

В 5% запросов слот уже занят (ответ 409), неизвестный пациент — 404.
Платёжный шлюз обычно отвечает за 150–400 мс, но в 10% запросов — за
2,5–3 с, а в 3% отклоняет платёж (ответ 402).

Пациенты (`patientId`):

| id | Пациент |
|---|---|
| `0a1b2c3d-1111-4a5b-8c9d-0e1f2a3b4c01` | Барсик, кот |
| `0a1b2c3d-2222-4a5b-8c9d-0e1f2a3b4c02` | Рекс, собака |
| `0a1b2c3d-3333-4a5b-8c9d-0e1f2a3b4c03` | Мурка, кошка |
| `0a1b2c3d-4444-4a5b-8c9d-0e1f2a3b4c04` | Арчи, собака |
| `0a1b2c3d-5555-4a5b-8c9d-0e1f2a3b4c05` | Кеша, кролик |

Трейс одного запроса:

```
appointments-api   POST appointments                                      SERVER
appointments-api     AppointmentsController.Create                        INTERNAL
appointments-api       AppointmentsService.CreateAsync                    INTERNAL
appointments-api         ScheduleClient.ReserveSlotAsync                  INTERNAL
appointments-api           clinic.ScheduleService/ReserveSlot             CLIENT
appointments-api             POST /clinic.ScheduleService/ReserveSlot     CLIENT
schedule-service               POST /clinic.ScheduleService/ReserveSlot   SERVER
schedule-service                 ScheduleGrpcHandler.ReserveSlot          INTERNAL
schedule-service                   SlotsRepository.ReserveAsync           INTERNAL
schedule-service                     UPDATE slots                         CLIENT
appointments-api         PatientsClient.GetMedicalCardAsync               INTERNAL
appointments-api           clinic.PatientsService/GetMedicalCard          CLIENT
appointments-api             POST /clinic.PatientsService/GetMedicalCard  CLIENT
patients-service               POST /clinic.PatientsService/GetMedicalCard SERVER
patients-service                 PatientsGrpcHandler.GetMedicalCard       INTERNAL
patients-service                   MedicalCardsRepository.FindAsync       INTERNAL
patients-service                     SELECT medical_cards                 CLIENT
appointments-api         PaymentsClient.ChargeAsync                       INTERNAL
appointments-api           POST /payments                                 CLIENT   ← дальше трейс не идёт
appointments-api         AppointmentsRepository.InsertAsync               INTERNAL
appointments-api           INSERT appointments                            CLIENT
```

## Концепции

- **Передача контекста по HTTP и gRPC** — `AddHttpClientInstrumentation()`
  в `appointments-api` создаёт клиентский спан на каждый исходящий запрос и
  добавляет в него заголовок `traceparent`. Клиент gRPC работает через тот
  же `HttpClient`, поэтому для gRPC это происходит так же. ASP.NET Core в
  вызываемом сервисе читает заголовок, и его серверный спан становится
  дочерним для клиентского спана вызывающего. Код сервисов про это ничего
  не знает.
- **Спаны gRPC-клиента** — библиотека `Grpc.Net.Client` на каждый вызов
  создаёт свой спан, а внутри него `HttpClient` делает HTTP/2-запрос. Если
  этот спан не экспортировать, спан HTTP-запроса ссылается на родителя,
  которого в трейсе нет, и вызов отрывается от трейса. Поэтому в
  `AppointmentsApi` подключён `AddGrpcClientInstrumentation()` (пакет
  `OpenTelemetry.Instrumentation.GrpcNetClient`, пока в статусе beta): спан
  gRPC-вызова экспортируется с именем `clinic.ScheduleService/ReserveSlot`, а
  под ним — спан HTTP-запроса.
- **Имена клиентских спанов HTTP** — по умолчанию спан исходящего
  HTTP-запроса называется только методом: `POST`. В `Program.cs` сервиса
  `AppointmentsApi` `EnrichWithHttpRequestMessage` добавляет путь:
  `POST /payments`, `POST /clinic.ScheduleService/ReserveSlot`.
- **Ручные спаны** — в каждом слое каждого сервиса: контроллер, сервис,
  клиенты, gRPC-обработчики, репозитории начинают свой спан
  (`AppTracing.Source.StartActivity("Класс.Метод")`), поэтому в трейсе видно,
  из каких шагов сложилось время запроса.
- **Внешняя система** — платёжный шлюз получает заголовок `traceparent`
  (пишет его в лог), но OpenTelemetry в нём нет, и трейс не продолжается. В
  трейсе остаётся только клиентский спан `POST /payments` в
  `appointments-api`: видно, сколько длился вызов и чем закончился.
- **Traces** → Service `appointments-api` → таблицы трейсов → клик по Trace ID →
  "Show trace on this dashboard": трейс со спанами всех сервисов;
- **Traces** → "Service graph" — граф вызовов;
- **Logs** → Service All — логи всех сервисов, в начале строки имя сервиса.

## Остановка

`Ctrl+C` в консолях сервисов и нагрузки. Общая инфраструктура
останавливается из папки `Lecture10.Tracing`: `docker compose down`.
