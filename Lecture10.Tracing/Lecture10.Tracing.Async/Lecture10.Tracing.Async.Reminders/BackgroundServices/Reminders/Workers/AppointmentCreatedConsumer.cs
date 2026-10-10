using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Lecture10.Tracing.Async.Contracts;
using Lecture10.Tracing.Async.Reminders.Repositories;
using Lecture10.Tracing.Async.Reminders.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;

namespace Lecture10.Tracing.Async.Reminders.BackgroundServices.Reminders.Workers;

public class AppointmentCreatedConsumer(IConfiguration configuration, IRemindersRepository repository, ILogger<AppointmentCreatedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig { BootstrapServers = configuration["Kafka:BootstrapServers"], GroupId = "reminders", AutoOffsetReset = AutoOffsetReset.Earliest }).Build();
        consumer.Subscribe("appointment.created");

        logger.LogInformation("Consumer group {GroupId} subscribed to {Topic}", "reminders", "appointment.created");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result is null)
            {
                continue;
            }

            await ProcessAsync(result);
        }

        consumer.Close();
    }

    private async Task ProcessAsync(ConsumeResult<string, string> result)
    {
        // Восстановление контекста: читаем из заголовка traceparent сообщения контекст спана send, отправленного другим сервисом
        var parent = Propagators.DefaultTextMapPropagator.Extract(                                                  // стандартный пропагатор W3C Trace Context, его настроил OpenTelemetry SDK
            default,                                                                                                // что вернуть, если заголовка с контекстом нет
            result.Message.Headers,                                                                                 // откуда читаем
            (headers, key) => headers.TryGetLastBytes(key, out var value) ? [Encoding.UTF8.GetString(value)] : []); // как прочитать один заголовок: байты в строку, нет заголовка — пусто

        // Спан обработки сообщения: родитель — спан send из сервиса-отправителя, а не текущий Activity.Current
        using var activity = AppTracing.Source.StartActivity( // начинаем спан и делаем его текущим
            "process appointment.created",                    // имя: операция и топик
            ActivityKind.Consumer,                            // вид: обработка сообщения
            parent.ActivityContext)                           // явный родитель из заголовков: тот же TraceId, родитель — SpanId спана send
            ?.SetTag("messaging.system", "kafka")
            .SetTag("messaging.operation.type", "process")
            .SetTag("messaging.destination.name", "appointment.created")
            .SetTag("messaging.consumer.group.name", "reminders")
            .SetTag("messaging.destination.partition.id", result.Partition.Value.ToString())
            .SetTag("messaging.kafka.offset", result.Offset.Value);

        var appointment = JsonSerializer.Deserialize<AppointmentCreated>(result.Message.Value)!;

        logger.LogInformation("appointment.created {AppointmentId} received from partition {Partition} at offset {Offset}, waited in topic {WaitedMs} ms", appointment.AppointmentId, result.Partition.Value, result.Offset.Value, (long)(DateTime.UtcNow - result.Message.Timestamp.UtcDateTime).TotalMilliseconds);

        await repository.InsertAsync(appointment.AppointmentId, appointment.PatientId, appointment.StartsAt.AddDays(-1));
    }
}
