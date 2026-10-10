using Clinic.Grpc;

namespace Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;

public interface IPatientsClient
{
    Task<MedicalCard?> GetMedicalCardAsync(Guid patientId);
}
