namespace Lecture10.Tracing.Async.Reminders.Repositories;

public interface IRemindersRepository
{
    Task InsertAsync(Guid appointmentId, Guid patientId, DateTime remindAt);
}
