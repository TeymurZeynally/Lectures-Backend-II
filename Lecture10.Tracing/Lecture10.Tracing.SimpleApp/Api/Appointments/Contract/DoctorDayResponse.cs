namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Contract;

public class DoctorDayResponse
{
    public required string Doctor { get; init; }
    public required string Specialization { get; init; }
    public required DateOnly Date { get; init; }
    public required List<AppointmentResponse> Appointments { get; init; }
}
