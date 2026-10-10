using Lecture10.Tracing.Async.Contracts;

namespace Lecture10.Tracing.Async.AppointmentsApi.External.Kafka;

public interface IAppointmentEventsProducer
{
    Task PublishCreatedAsync(AppointmentCreated appointment);
}
