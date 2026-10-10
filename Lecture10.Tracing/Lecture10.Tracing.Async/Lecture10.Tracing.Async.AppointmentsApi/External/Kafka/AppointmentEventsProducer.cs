using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Lecture10.Tracing.Async.AppointmentsApi.Tracing;
using Lecture10.Tracing.Async.Contracts;
using OpenTelemetry.Context.Propagation;

namespace Lecture10.Tracing.Async.AppointmentsApi.External.Kafka;

public class AppointmentEventsProducer : IAppointmentEventsProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<AppointmentEventsProducer> _logger;

    public AppointmentEventsProducer(IProducer<string, string> producer, ILogger<AppointmentEventsProducer> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishCreatedAsync(AppointmentCreated appointment)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentEventsProducer.PublishCreatedAsync");

        using var send = AppTracing.Source.StartActivity("send appointment.created", ActivityKind.Producer)
            ?.SetTag("messaging.system", "kafka")
            .SetTag("messaging.operation.type", "send")
            .SetTag("messaging.destination.name", "appointment.created")
            .SetTag("messaging.kafka.message.key", appointment.AppointmentId.ToString());

        // Передача контекста: записываем контекст спана send в заголовок traceparent сообщения Kafka
        var headers = new Headers();                                                   // пустые заголовки сообщения Kafka
        Propagators.DefaultTextMapPropagator.Inject(                                   // стандартный пропагатор W3C Trace Context, его настроил OpenTelemetry SDK
            new PropagationContext(send?.Context ?? default, default),                 // что передаём: TraceId и SpanId спана send (второй аргумент не используем)
            headers,                                                                   // куда записываем
            (message, key, value) => message.Add(key, Encoding.UTF8.GetBytes(value))); // как записать один заголовок: Kafka хранит значения в байтах

        var result = await _producer.ProduceAsync(
            "appointment.created",
            new Message<string, string> { Key = appointment.AppointmentId.ToString(), Value = JsonSerializer.Serialize(appointment),
            Headers = headers });

        send?.SetTag("messaging.destination.partition.id", result.Partition.Value.ToString()).SetTag("messaging.kafka.offset", result.Offset.Value);

        _logger.LogInformation("appointment.created {AppointmentId} sent to partition {Partition} at offset {Offset}, traceparent header: {Traceparent}", appointment.AppointmentId, result.Partition.Value, result.Offset.Value, headers.TryGetLastBytes("traceparent", out var traceparent) ? Encoding.UTF8.GetString(traceparent) : "-");
    }
}
