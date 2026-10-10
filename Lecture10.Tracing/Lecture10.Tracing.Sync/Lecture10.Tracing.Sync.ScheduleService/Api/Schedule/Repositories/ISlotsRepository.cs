namespace Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.Repositories;

public interface ISlotsRepository
{
    Task<Guid?> ReserveAsync(Guid doctorId, DateTime startsAt);
}
