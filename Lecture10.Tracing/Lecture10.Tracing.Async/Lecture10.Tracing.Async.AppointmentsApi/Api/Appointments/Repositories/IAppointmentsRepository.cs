namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Repositories;

public interface IAppointmentsRepository
{
    Task InsertAsync(Guid appointmentId, Guid doctorId, Guid patientId, DateTime startsAt);
}
