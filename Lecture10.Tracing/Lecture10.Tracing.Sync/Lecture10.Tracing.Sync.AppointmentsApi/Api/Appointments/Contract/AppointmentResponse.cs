namespace Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Contract;

public class AppointmentResponse
{
    public required Guid Id { get; init; }
    public required Guid DoctorId { get; init; }
    public required string Patient { get; init; }
    public required string Owner { get; init; }
    public required DateTime StartsAt { get; init; }
    public required Guid SlotId { get; init; }
    public required Guid PaymentId { get; init; }
}
