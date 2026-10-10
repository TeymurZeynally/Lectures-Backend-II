using Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Contract;
using Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Repositories;
using Lecture10.Tracing.Async.AppointmentsApi.External.Kafka;
using Lecture10.Tracing.Async.AppointmentsApi.Tracing;
using Lecture10.Tracing.Async.Contracts;

namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly IAppointmentsRepository _repository;
    private readonly IAppointmentEventsProducer _producer;
    private readonly ILogger<AppointmentsService> _logger;

    public AppointmentsService(IAppointmentsRepository repository, IAppointmentEventsProducer producer, ILogger<AppointmentsService> logger)
    {
        _repository = repository;
        _producer = producer;
        _logger = logger;
    }

    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsService.CreateAsync");

        await Task.Delay(Random.Shared.Next(40, 60));

        var appointmentId = Guid.NewGuid();

        await _repository.InsertAsync(appointmentId, request.DoctorId, request.PatientId, request.StartsAt);

        await _producer.PublishCreatedAsync(new AppointmentCreated(appointmentId, request.DoctorId, request.PatientId, request.StartsAt));

        _logger.LogInformation("Appointment {AppointmentId} created, notifications and reminders will be processed asynchronously", appointmentId);

        return new AppointmentResponse { Id = appointmentId, DoctorId = request.DoctorId, PatientId = request.PatientId, StartsAt = request.StartsAt };
    }
}
