using System.Diagnostics;
using Lecture10.Tracing.Async.Reminders.Tracing;
using Microsoft.Extensions.Logging;

namespace Lecture10.Tracing.Async.Reminders.Repositories;

public class RemindersRepository(ILogger<RemindersRepository> logger) : IRemindersRepository
{
    public async Task InsertAsync(Guid appointmentId, Guid patientId, DateTime remindAt)
    {
        using var activity = AppTracing.Source.StartActivity("RemindersRepository.InsertAsync");

        using var query = AppTracing.Source.StartActivity("INSERT reminders", ActivityKind.Client)
            ?.SetTag("db.system.name", "postgresql")
            .SetTag("db.namespace", "reminders-db")
            .SetTag("db.query.text", "INSERT INTO reminders (appointment_id, patient_id, remind_at) VALUES ($1, $2, $3)");

        var elapsedMs = Random.Shared.Next(20, 50);
        await Task.Delay(elapsedMs);

        if (Random.Shared.NextDouble() < 0.02)
        {
            var exception = new TimeoutException("Timeout during reading attempt");
            query?.AddException(exception);
            query?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "INSERT reminders failed after {ElapsedMs} ms: the database did not respond, reminder for appointment {AppointmentId} is not saved", elapsedMs, appointmentId);
            return;
        }

        query?.SetTag("db.response.returned_rows", 1);

        logger.LogInformation("Reminder for appointment {AppointmentId} scheduled at {RemindAt:yyyy-MM-dd HH:mm}, INSERT reminders completed in {ElapsedMs} ms", appointmentId, remindAt, elapsedMs);
    }
}
