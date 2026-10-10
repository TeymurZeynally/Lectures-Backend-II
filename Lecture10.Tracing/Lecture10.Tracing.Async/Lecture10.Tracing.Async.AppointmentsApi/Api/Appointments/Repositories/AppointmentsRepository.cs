using System.Diagnostics;
using Lecture10.Tracing.Async.AppointmentsApi.Tracing;

namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Repositories;

public class AppointmentsRepository : IAppointmentsRepository
{
    private readonly ILogger<AppointmentsRepository> _logger;

    public AppointmentsRepository(ILogger<AppointmentsRepository> logger)
    {
        _logger = logger;
    }

    public async Task InsertAsync(Guid appointmentId, Guid doctorId, Guid patientId, DateTime startsAt)
    {
        using var activity = AppTracing.Source.StartActivity("AppointmentsRepository.InsertAsync");

        using var query = AppTracing.Source.StartActivity("INSERT appointments", ActivityKind.Client)
            ?.SetTag("db.system.name", "postgresql")
            .SetTag("db.namespace", "clinic-db")
            .SetTag("db.query.text", "INSERT INTO appointments (id, doctor_id, patient_id, starts_at) VALUES ($1, $2, $3, $4)");

        var elapsedMs = Random.Shared.Next(15, 35);
        await Task.Delay(elapsedMs);

        query?.SetTag("db.response.returned_rows", 1);

        _logger.LogInformation("INSERT appointments completed in {ElapsedMs} ms, appointment {AppointmentId}", elapsedMs, appointmentId);
    }
}
