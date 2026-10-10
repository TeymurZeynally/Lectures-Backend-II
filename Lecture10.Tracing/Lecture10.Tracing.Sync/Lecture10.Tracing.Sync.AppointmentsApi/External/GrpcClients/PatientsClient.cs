using Clinic.Grpc;
using Grpc.Core;
using Lecture10.Tracing.Sync.AppointmentsApi.Tracing;

namespace Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;

public class PatientsClient : IPatientsClient
{
    private readonly PatientsService.PatientsServiceClient _client;

    public PatientsClient(PatientsService.PatientsServiceClient client)
    {
        _client = client;
    }

    public async Task<MedicalCard?> GetMedicalCardAsync(Guid patientId)
    {
        using var activity = AppTracing.Source.StartActivity("PatientsClient.GetMedicalCardAsync");

        try
        {
            return await _client.GetMedicalCardAsync(new GetMedicalCardRequest { PatientId = patientId.ToString() });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
