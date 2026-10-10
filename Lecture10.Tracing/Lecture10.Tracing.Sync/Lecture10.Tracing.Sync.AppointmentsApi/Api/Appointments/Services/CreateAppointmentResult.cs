using Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Contract;

namespace Lecture10.Tracing.Sync.AppointmentsApi.Api.Appointments.Services;

public enum CreateAppointmentStatus
{
    Created,
    SlotTaken,
    PatientNotFound,
    PaymentDeclined
}

public record CreateAppointmentResult(CreateAppointmentStatus Status, AppointmentResponse? Appointment = null);
