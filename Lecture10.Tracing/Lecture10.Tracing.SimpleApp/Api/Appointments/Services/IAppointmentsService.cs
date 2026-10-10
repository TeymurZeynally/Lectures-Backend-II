using Lecture10.Tracing.SimpleApp.Api.Appointments.Contract;

namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Services;

public interface IAppointmentsService
{
    Task<DoctorDayResponse?> GetDoctorDayAsync(Guid doctorId, DateOnly date);
}
