namespace Lecture10.Tracing.SimpleApp.Api.Appointments.Repositories.Entities;

public record Appointment(Guid Id, Guid PatientId, string PatientName, DateTime StartsAt, string Reason);
