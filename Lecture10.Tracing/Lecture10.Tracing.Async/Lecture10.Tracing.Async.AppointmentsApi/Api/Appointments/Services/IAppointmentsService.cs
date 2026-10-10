using Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Contract;

namespace Lecture10.Tracing.Async.AppointmentsApi.Api.Appointments.Services;

public interface IAppointmentsService
{
    Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request);
}
