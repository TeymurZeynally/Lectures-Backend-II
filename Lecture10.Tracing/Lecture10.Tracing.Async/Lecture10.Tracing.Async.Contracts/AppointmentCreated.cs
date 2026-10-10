namespace Lecture10.Tracing.Async.Contracts;

public record AppointmentCreated(Guid AppointmentId, Guid DoctorId, Guid PatientId, DateTime StartsAt);
