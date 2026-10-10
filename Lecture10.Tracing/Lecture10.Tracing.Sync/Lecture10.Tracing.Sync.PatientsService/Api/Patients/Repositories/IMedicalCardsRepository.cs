using Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories.Entities;

namespace Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories;

public interface IMedicalCardsRepository
{
    Task<Patient?> FindAsync(Guid patientId);
}
