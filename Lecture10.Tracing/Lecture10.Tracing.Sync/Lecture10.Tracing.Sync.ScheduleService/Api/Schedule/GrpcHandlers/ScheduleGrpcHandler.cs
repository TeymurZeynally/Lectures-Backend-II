using Clinic.Grpc;
using Grpc.Core;
using Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.Repositories;
using Lecture10.Tracing.Sync.ScheduleService.Tracing;

namespace Lecture10.Tracing.Sync.ScheduleService.Api.Schedule.GrpcHandlers;

public class ScheduleGrpcHandler : Clinic.Grpc.ScheduleService.ScheduleServiceBase
{
    private readonly ISlotsRepository _repository;
    private readonly ILogger<ScheduleGrpcHandler> _logger;

    public ScheduleGrpcHandler(ISlotsRepository repository, ILogger<ScheduleGrpcHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<ReserveSlotResponse> ReserveSlot(ReserveSlotRequest request, ServerCallContext context)
    {
        using var activity = AppTracing.Source.StartActivity("ScheduleGrpcHandler.ReserveSlot");

        _logger.LogInformation("Reserving slot for doctor {DoctorId} at {StartsAt}", request.DoctorId, request.StartsAt);

        var slotId = await _repository.ReserveAsync(Guid.Parse(request.DoctorId), DateTime.Parse(request.StartsAt));
        if (slotId is null)
        {
            _logger.LogWarning("Slot of doctor {DoctorId} at {StartsAt} is already taken", request.DoctorId, request.StartsAt);
            throw new RpcException(new Status(StatusCode.AlreadyExists, "Slot is already taken"));
        }

        _logger.LogInformation("Slot {SlotId} reserved for doctor {DoctorId} at {StartsAt}", slotId, request.DoctorId, request.StartsAt);

        return new ReserveSlotResponse { SlotId = slotId.ToString() };
    }
}
