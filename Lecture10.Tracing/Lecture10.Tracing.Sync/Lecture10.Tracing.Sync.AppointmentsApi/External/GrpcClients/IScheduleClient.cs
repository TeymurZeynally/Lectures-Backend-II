namespace Lecture10.Tracing.Sync.AppointmentsApi.External.GrpcClients;

public interface IScheduleClient
{
    Task<Guid?> ReserveSlotAsync(Guid doctorId, DateTime startsAt);
}
