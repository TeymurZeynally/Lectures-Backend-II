namespace Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories.Entities;

public record Patient(Guid Id, string Name, string Species, string Owner);
