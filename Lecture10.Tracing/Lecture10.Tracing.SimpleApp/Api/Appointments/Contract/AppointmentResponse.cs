namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Contract;

public class AppointmentResponse
{
    public required Guid Id { get; init; }
    public required TimeOnly Time { get; init; }
    public required string Patient { get; init; }
    public required string Reason { get; init; }
}
