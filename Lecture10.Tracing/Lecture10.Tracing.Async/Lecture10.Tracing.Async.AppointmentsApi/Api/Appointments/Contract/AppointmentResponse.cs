namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Contract;

public class AppointmentResponse
{
    public required Guid Id { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid PatientId { get; init; }
    public required DateTime StartsAt { get; init; }
}
