namespace Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Contract;

public class CreateAppointmentRequest
{
    public required Guid DoctorId { get; init; }
    public required Guid PatientId { get; init; }
    public required DateTime StartsAt { get; init; }
}
