using Clinic.Grpc;
using Grpc.Core;
using Lecture10.Tracing.Sync.PatientsService.Api.Patients.Repositories;
using Lecture10.Tracing.Sync.PatientsService.Tracing;

namespace Lecture10.Tracing.Sync.PatientsService.Api.Patients.GrpcHandlers;

public class PatientsGrpcHandler : Clinic.Grpc.PatientsService.PatientsServiceBase
{
    private readonly IMedicalCardsRepository _repository;
    private readonly ILogger<PatientsGrpcHandler> _logger;

    public PatientsGrpcHandler(IMedicalCardsRepository repository, ILogger<PatientsGrpcHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<MedicalCard> GetMedicalCard(GetMedicalCardRequest request, ServerCallContext context)
    {
        using var activity = AppTracing.Source.StartActivity("PatientsGrpcHandler.GetMedicalCard");

        _logger.LogInformation("Medical card of patient {PatientId} requested", request.PatientId);

        var card = await _repository.FindAsync(Guid.Parse(request.PatientId));
        if (card is null)
        {
            _logger.LogWarning("Patient {PatientId} not found", request.PatientId);
            throw new RpcException(new Status(StatusCode.NotFound, $"Patient {request.PatientId} not found"));
        }

        _logger.LogInformation("Medical card of patient {Patient} ({Species}) found, owner {Owner}", card.Name, card.Species, card.Owner);

        return new MedicalCard { PatientId = card.Id.ToString(), Name = card.Name, Species = card.Species, Owner = card.Owner };
    }
}
