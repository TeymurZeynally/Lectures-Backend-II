using Clinic.Grpc;
using Grpc.Core;
using Lecture10.Tracing.Sync.AppointmentsApi.Tracing;

namespace Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;

public class ScheduleClient : IScheduleClient
{
    private readonly ScheduleService.ScheduleServiceClient _client;

    public ScheduleClient(ScheduleService.ScheduleServiceClient client)
    {
        _client = client;
    }

    public async Task<Guid?> ReserveSlotAsync(Guid doctorId, DateTime startsAt)
    {
        using var activity = AppTracing.Source.StartActivity("ScheduleClient.ReserveSlotAsync");

        try
        {
            var response = await _client.ReserveSlotAsync(new ReserveSlotRequest { DoctorId = doctorId.ToString(), StartsAt = startsAt.ToString("O") });
            return Guid.Parse(response.SlotId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            return null;
        }
    }
}
