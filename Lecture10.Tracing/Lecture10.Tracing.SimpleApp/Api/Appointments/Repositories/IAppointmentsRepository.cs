using Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories.Entities;

namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories;

public interface IAppointmentsRepository
{
    Task<Doctor?> GetDoctorAsync(Guid doctorId);

    Task<List<Appointment>> FindByDoctorAndDateAsync(Guid doctorId, DateOnly date);
}
