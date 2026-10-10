using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Contract;

namespace Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Services;

public interface IAppointmentsService
{
    Task<CreateAppointmentResult> CreateAsync(CreateAppointmentRequest request);
}
